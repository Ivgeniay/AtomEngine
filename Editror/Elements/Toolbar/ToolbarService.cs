using System.Collections.Generic;
using System.Threading.Tasks;

namespace Editor
{
    public class ToolbarService : IService
    {
        private EditorToolbar? _editorToolbar;
        internal void RegisterEditorToolbar(EditorToolbar editorToolbar) => _editorToolbar = editorToolbar;
        public Task InitializeAsync() => Task.CompletedTask;
        public void RegisterCathegory(EditorToolbarCategory category) => _editorToolbar?.RegisterCathegory(category);
        public void UpdateToolbar() => _editorToolbar?.UpdateToolbar();
        public IEnumerable<EditorToolbarCategory> GetEditorData() => _editorToolbar?.GetEditorData();
        public void CreateMenuButton(EditorToolbarCategory category, EditorToolbarButton button) => _editorToolbar?.CreateMenuButton(category, button);
    }
}
