using PCheck.Models;

namespace PCheck.Util.Repository;

public class Policy : EntityBase
{
    public string PolicyId { get; set; } = string.Empty;
    public float[] ContentToLookFor {get; set;} = new float[1536];
    public float OptimalDistance {get;set;} = 0.001f;
    public float OffTopicDistance {get; set;} = 0.001f;

}

public enum ExistingPolicies {
    Auth_KnowHow_01,
    Auth_KnowHow_02,
    Auth_Ownership_01,
    Auth_Ownership_02
   
}

public abstract class EntityBase 
{
    public string Id { get; set; }

    public EntityBase()
    {
        Id = Guid.NewGuid().ToString();
    }
}

public interface IRepository<T> where T : EntityBase
{
    Task<T> GetById(string id);
    Task<T> Create(T entity);
    Task<bool> Delete(string id);
}
