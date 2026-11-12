using Repository.Repositories;
namespace Services;

public abstract class GenericServices<TEntity> where TEntity : class
{
    private GenericRepository<TEntity> _repository;

    public GenericServices(GenericRepository<TEntity> repository)
    {
        _repository = repository;
    }

    public List<TEntity> GetAll()
    {
        return _repository.getAll();
    }

    public TEntity? GetById(int? id)
    {
        return id.HasValue ? _repository.getById(id.Value) : null;
    }

    public virtual bool Insert(TEntity entity)
    {
        bool resp = false;
        if (entity != null)
        {
            _repository.Insert(entity);
            _repository.Persist(entity);
            resp = true;
        }
        return resp;
    }
    public virtual bool Update(TEntity entity)
    {
        bool resp = false;
        if (entity != null)
        {
            _repository.Update(entity);
            _repository.Persist(entity);
            resp = true;
        }
        return resp;
    }

    public bool Delete(TEntity entity)
    {
        bool resp = false;
        if (entity != null)
        {
            _repository.Delete(entity);
            _repository.Persist(entity);
            resp = true;
        }
        return resp;
    }

    

}
