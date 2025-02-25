using System;
using System.Collections.Generic;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Diagnostics;
using AtomEngine;
using System.Linq;

namespace Editor
{
    internal static class InspectorDistributor
    {
        private static ProjectScene _currentScene;

        public static void Initialize(ProjectScene projectScene) => _currentScene = projectScene;

        public static IInspectable GetInspectable(object source)
        {
            switch (source)
            {
                case EntityHierarchyItem hierarchyItem:
                    var entityData = _currentScene.CurrentWorldData.Entities.Where(e => e.Id == hierarchyItem.Id && e.Version == hierarchyItem.Version).FirstOrDefault();
                    var _entity = new Entity(entityData.Id, entityData.Version);
                    var collection = entityData.Components.Values.ToList();
                    if (collection == null) collection = new List<IComponent>();
                    var inscted = new EntityInspectable(_entity, collection);
                    return inscted;

                default:
                    DebLogger.Warn($"Нераспознаный тип для испекции: {source.GetType()}");
                    return null;
            }
        }

    }
}
