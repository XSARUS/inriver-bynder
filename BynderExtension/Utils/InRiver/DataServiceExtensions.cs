using inRiver.Remoting;
using inRiver.Remoting.Objects;

namespace Bynder.Utils.InRiver
{
    public static class DataServiceExtensions
    {
        /// <summary>
        /// check if link already exists, otherwise create and return it
        /// if link already exists, null will be the answer
        /// </summary>
        /// <param name="dataService"></param>
        /// <param name="link"></param>
        /// <returns></returns>
        public static Link CreateLinkIfNotExists(this IDataService dataService, Link link)
        {
            return dataService.LinkAlreadyExists(link.Source.Id, link.Target.Id, link.LinkEntity?.Id, link.LinkType.Id) 
                ? null 
                : dataService.AddLinkLast(link);
        }

        /// <summary>
        /// check if link already exists, otherwise create and return it
        /// if link already exists, null will be the answer
        /// </summary>
        /// <param name="dataService"></param>
        /// <param name="sourceEntity"></param>
        /// <param name="targetEntity"></param>
        /// <param name="linkType"></param>
        /// <returns></returns>
        public static Link CreateLinkIfNotExists(this IDataService dataService, Entity sourceEntity,
            Entity targetEntity, LinkType linkType)
        {
            return dataService.CreateLinkIfNotExists(new Link
            {
                LinkType = linkType,
                Source = sourceEntity,
                Target = targetEntity
            });
        }

        /// <summary>
        /// try get entity from repository but only if type matches
        /// </summary>
        /// <param name="dataService"></param>
        /// <param name="entityTypeId"></param>
        /// <param name="entityId"></param>
        /// <param name="loadLevel"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static bool TryGetEntityOfType(this IDataService dataService, int entityId,
            LoadLevel loadLevel, string entityTypeId, out Entity entity)
        {
            entity = dataService.GetEntity(entityId, loadLevel);
            return entity != null && entity.EntityType.Id.Equals(entityTypeId);
        }

        /// <summary>
        /// loads the entity with the required loadlevel
        /// </summary>
        /// <param name="dataService"></param>
        /// <param name="entity"></param>
        /// <param name="loadLevel"></param>
        /// <returns></returns>
        public static Entity EntityLoadLevel(this IDataService dataService, Entity entity, LoadLevel loadLevel)
        {
            return entity.LoadLevel < loadLevel 
                ? dataService.GetEntity(entity.Id, loadLevel) 
                : entity;
        }
    }
}
