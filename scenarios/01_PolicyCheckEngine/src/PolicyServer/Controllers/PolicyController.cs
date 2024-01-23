using Microsoft.AspNetCore.Mvc;
using MathNet.Numerics;
using SKit.Scenario;
using SKit.Scenario.PolicyCheck.Models;
using SKit.Scenario.PolicyCheck.Util;

namespace SKit.Scenario.PolicyCheck.Controllers;

[ApiController]
public class PolicyController : ControllerBase
{
    private readonly ILogger<PolicyController> _logger;

    public PolicyController(ILogger<PolicyController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    [Route("/policyrulecheck/{policyId}/{content}")]
    public async Task<ActionResult<PolicyRuleCheckResponse<PolicyRuleCheckResult>>> CheckPolicyRule(
        IOpenAI openAI,
        IRepository<PolicyRule> policyRepository,
        string policyId, 
        string content) {
        //content = transcript with questions raised during a conversation

        PolicyRuleCheckResponse<PolicyRuleCheckResult> response = new PolicyRuleCheckResponse<PolicyRuleCheckResult>();

        PolicyRuleCheckRequest<CheckRequest> request = new PolicyCheck.Models.PolicyRuleCheckRequest<CheckRequest>() {
            RequestData = new CheckRequest() {
                PolicyId = policyId,
                Content = content
            }
        };

        // retrieve pre-calculated policy data from repository
        PolicyRule policy = await policyRepository.GetById(request.RequestData.PolicyId);
        if (policy == null) {
            response.Status = PolicyRuleCheckResponseStatus.Failure;
            response.Error.Add($"policy not found: {request.RequestData.PolicyId}");
            return StatusCode(StatusCodes.Status404NotFound, response);  
        }

        //create embedding for content (data which should be checked against policy)
        (bool success, float[] contentVector) = await openAI.CreateEmbedding(request.RequestData.Content);
        if (!success) {
            response.Status = PolicyRuleCheckResponseStatus.Failure;
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
    [Route("/policyrulecheck")]
    public async Task<ActionResult<PolicyRuleCheckResponse<PolicyRuleCheckResult>>> RegisterPolicyRule(
        IOpenAI openAI,
        IRepository<PolicyRule> policyRepository,
        [FromBody]PolicyRuleCheckRequest<List<PolicyRuleRegisterRequest>> request) {

        PolicyRuleCheckResponse<PolicyRuleRegisterResult> response = new PolicyRuleCheckResponse<PolicyRuleRegisterResult>();

        foreach(PolicyRuleRegisterRequest policyRegisterRequest in request.RequestData) {

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
            await policyRepository.Create(new PolicyRule() {
                PolicyId = policyRegisterRequest.PolicyId,
                ContentToLookFor = vectorContentToLookFor,
                AvgOptimalDistance = potPhrasesDistances / policyRegisterRequest.PotentialPhrases.Length,
                AvgOffTopicDistance = offTopicDistances / policyRegisterRequest.OffTopicPhrases.Length
            });

        }
        response.Result.PolicyUris = request.RequestData.Select(item => $"/policyrulecheck/{item.PolicyId}/").ToArray();

        return Ok(response);
    }
}
