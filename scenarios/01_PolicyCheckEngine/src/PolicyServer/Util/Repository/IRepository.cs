using static SKit.Scenario.PolicyCheck.Util.PolicyRepository;

namespace SKit.Scenario.PolicyCheck.Util;

public interface IRepository<T> where T : EntityBase
{
    Task<T> GetById(string id);
    Task<T> Create(T entity);
    Task<bool> Delete(string id);
}
