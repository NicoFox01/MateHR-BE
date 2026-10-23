namespace MateHR.Domain.Recruitment.Exceptions
{
    public sealed class EntityInUseException : Exception
    {
        public EntityInUseException(string entity, Guid id)
            : base($"No se puede eliminar {entity} {id} porque esta en uso.") { }
    }
}
