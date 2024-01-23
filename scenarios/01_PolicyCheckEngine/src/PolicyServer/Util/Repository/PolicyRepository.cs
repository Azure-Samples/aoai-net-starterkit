namespace PCheck.Util;

using System.Text.Json;
using PCheck.Models;

public class PolicyRepository : IRepository<Policy>
{
    Dictionary<string, Policy> _policyRepository = new Dictionary<string, Policy>();

    public PolicyRepository(string dataFolder)
    {
        // pre-load policies
        foreach (string fileName in Directory.GetFiles(dataFolder, "*.json"))
            AddPolicyFromFile(fileName, Path.GetFileNameWithoutExtension(fileName));
    }

    public async Task<Policy> GetById(string id)
    {
        return await Task.Run( () => _policyRepository.Where(item => item.Key == id).FirstOrDefault().Value);
    }

    public async Task<Policy> Create(Policy item)
    {
        // delete if exists
        if (_policyRepository.ContainsKey(item.PolicyId))
            await Delete(item.PolicyId);

        // add to repository
        await Task.Run( () => _policyRepository.Add(item.PolicyId, item));
        return item;
    }

    public async Task<bool> Delete(string id)
    {
        return await Task.Run( () => _policyRepository.Remove(id));     
    }

    void AddPolicyFromFile(string fileName, string policy) {

        JsonDocument jsonDocument = JsonDocument.Parse(
            File.ReadAllText(fileName));

        _policyRepository.Add(policy.ToString(),
            new Policy()
            {
                PolicyId = policy,
                ContentToLookFor = jsonDocument
                    .RootElement.GetProperty("vectorContentToLookFor")
                    .EnumerateArray()
                    .Select(item => item.GetSingle())
                    .ToArray(), 
                AvgOptimalDistance = jsonDocument
                    .RootElement.GetProperty("avgOptimalDistance").GetSingle(),
                AvgOffTopicDistance = jsonDocument
                    .RootElement.GetProperty("avgOffTopicDistance").GetSingle()
            }
        );
    }

    
    public abstract class EntityBase 
    {
        public string Id { get; set; }

        public EntityBase()
        {
            Id = Guid.NewGuid().ToString();
        }
    }
}

