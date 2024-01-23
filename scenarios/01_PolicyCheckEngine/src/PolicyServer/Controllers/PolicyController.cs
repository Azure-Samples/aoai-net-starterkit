using Microsoft.AspNetCore.Mvc;
using MathNet.Numerics;
using PCheck.Models;
using PCheck.Util;

namespace PC.Controllers;

[ApiController]
public class PolicyController : ControllerBase
{
    private readonly ILogger<PolicyController> _logger;

    public PolicyController(ILogger<PolicyController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    [Route("/policycheck/{policyId}/{content}")]
    public async Task<ActionResult<PCheckResponse<PolicyCheckResult>>> CheckPolicy(
        IOpenAI openAI,
        IRepository<Policy> policyRepository,
        string policyId, 
        string content) {
        //content = transcript with questions raised during a conversation

        PCheckResponse<PolicyCheckResult> response = new PCheckResponse<PolicyCheckResult>();

        PCheckRequest<CheckRequest> request = new PCheck.Models.PCheckRequest<CheckRequest>() {
            RequestData = new CheckRequest() {
                PolicyId = policyId,
                Content = content
            }
        };

        // retrieve pre-calculated policy data from repository
        Policy policy = await policyRepository.GetById(request.RequestData.PolicyId);
        if (policy == null) {
            response.Status = PCheckResponseStatus.Failure;
            response.Error.Add($"policy not found: {request.RequestData.PolicyId}");
            return StatusCode(StatusCodes.Status404NotFound, response);  
        }

        //create embedding for content (data which should be checked against policy)
        (bool success, float[] contentVector) = await openAI.CreateEmbedding(request.RequestData.Content);
        if (!success) {
            response.Status = PCheckResponseStatus.Failure;
            response.Error.Add($"failed to create embedding for content: {request.RequestData.Content.Substring(10)}...");
            return StatusCode(StatusCodes.Status500InternalServerError, response);  
        }

        //calculate cosine distance and deviation from avg optimal distance
        float cosineDistance = Distance.Cosine(contentVector, policy.ContentToLookFor);

        //calculate absolute difference between cosineDistance and policy.OptimalDistance
        float differenceOptimalDistance = Math.Abs(cosineDistance - policy.AvgOptimalDistance);
        float differenceOffTopicDistance = Math.Abs(cosineDistance - policy.AvgOffTopicDistance);
        
        response.Result.DeviationOptimalDistance = (differenceOptimalDistance / policy.AvgOptimalDistance) * 100;
        if (cosineDistance > policy.AvgOptimalDistance) {
            response.Result.DeviationOptimalDistance *= -1;
        }
        response.Result.DeviationOffTopicDistance = (differenceOffTopicDistance / policy.AvgOptimalDistance) * 100;
        if (cosineDistance > policy.AvgOffTopicDistance) {
            response.Result.DeviationOffTopicDistance *= -1;
        }

        response.Result.CalculatedDistance = cosineDistance;
        response.Result.AvgOptimalDistance = policy.AvgOptimalDistance;
        response.Result.AvgOffTopicDistance = policy.AvgOffTopicDistance;
        
        return Ok(response);
    }

    [HttpPut]
    [Route("/policycheck")]
    public async Task<ActionResult<PCheckResponse<PolicyCheckResult>>> RegisterPolicy(
        IOpenAI openAI,
        IRepository<Policy> policyRepository,
        [FromBody]PCheckRequest<List<PolicyRegisterRequest>> request) {

        PCheckResponse<PolicyRegisterResult> response = new PCheckResponse<PolicyRegisterResult>();

        foreach(PolicyRegisterRequest policyRegisterRequest in request.RequestData) {

            //create embedding for "Content to look for" & calcualte "Potential Phrases" distance
            (bool success, float[] vectorContentToLookFor) = await openAI.CreateEmbedding(policyRegisterRequest.ContentToLookFor);
            float potPhrasesDistances = 0f; 
            foreach(string potentialPhrase in policyRegisterRequest.PotentialPhrases) {
                (success, float[] vectorPotentialPhrase) = await openAI.CreateEmbedding(potentialPhrase);
                potPhrasesDistances += Distance.Cosine(vectorContentToLookFor, vectorPotentialPhrase);
            }

            //create embedding for "offTopic" & calculate "Content to look for" distance
            float offTopicDistances = 0f;
            foreach(string offTopic in policyRegisterRequest.OffTopicPhrases) {
                (success, float[] vectorOffTopic) = await openAI.CreateEmbedding(offTopic);
                offTopicDistances += Distance.Cosine(vectorContentToLookFor, vectorOffTopic);
            }
            
            //store policy (including avg. distance for PotentialPhrases and OffTopicPhrases)
            await policyRepository.Create(new Policy() {
                PolicyId = policyRegisterRequest.PolicyId,
                ContentToLookFor = vectorContentToLookFor,
                AvgOptimalDistance = potPhrasesDistances / policyRegisterRequest.PotentialPhrases.Length,
                AvgOffTopicDistance = offTopicDistances / policyRegisterRequest.OffTopicPhrases.Length
            });

        }
        response.Result.PolicyUris = request.RequestData.Select(item => $"/policycheck/{item.PolicyId}/").ToArray();

        return Ok(response);
    }
}
