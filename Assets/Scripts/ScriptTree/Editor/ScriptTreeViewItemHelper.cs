using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptTree
{
    public class ScriptItemView
    {
        public int id;
        public string display;
        public bool canRemove = true;
        public bool canRename = true;
        public ScriptItemView parent;
        public List<ScriptItemView> children = new List<ScriptItemView>();
        public Action<ScriptItemView, ScriptTreeView> onClick;
        public object data;

        public void AddChild(ScriptItemView item)
        {
            children.Add(item);
            item.parent = this;
        }
    }

    public static class ScriptTreeViewItemHelper
    {
        private static int counter = 0;

        public static void ResetCounter() => counter = 0;

        private static ScriptItemView NewScriptItemView(string title = null)
        {
            ScriptItemView view = new ScriptItemView();
            view.id = ++counter;
            view.display = title ?? $"#{counter}";

            return view;
        }

        public static ScriptItemView BuildIfStruct(IfStatNode node, ScriptItemView view = null)
        {
            ScriptItemView root;
            if (view != null)
            {
                view.display = "#if";
                root = view;
            }
            else
            {
                root = NewScriptItemView("#if");
            }
            var def = NewScriptItemView("default");
            var caseInserter = BuildInserter(BuildCaseStructOverView, "$newCase");

            def.AddChild(BuildInserter());

            root.data = node;
            def.data = node;
            caseInserter.data = node;
            root.AddChild(caseInserter);
            root.AddChild(def);

            def.canRemove = false;
            return root;
        }

        private static void BuildCaseStructOverView(ScriptItemView view)
        {
            view.children.Clear();
            view.display = "#case";
            var condition = NewScriptItemView("condition: Equal");
            var stats = NewScriptItemView("stats");

            condition.AddChild(BuildParameter("literal: \"a\""));
            condition.AddChild(BuildParameter("literal: \"a\""));

            stats.AddChild(BuildInserter());

            view.AddChild(condition);
            view.AddChild(stats);

            condition.data = view.data;
            stats.data = view.data;

            condition.canRemove = false;
            stats.canRemove = false;
        }

        private static ScriptItemView BuildParameter(string paramName)
        {
            var root = NewScriptItemView(paramName);
            root.canRemove = false;
            root.canRename = false;

            return root;
        }


        public static ScriptItemView BuildInserter(Action<ScriptItemView> init = null, string title = null)
        {
            var addItem = NewScriptItemView();
            addItem.canRemove = false;
            addItem.display = title ?? "$...";
            addItem.onClick += (ScriptItemView item, ScriptTreeView view) =>
            {
                var index = item.parent.children.IndexOf(item);
                var newitem = NewScriptItemView();
                newitem.parent = item.parent;
                item.parent.children.Insert(index, newitem);
                init?.Invoke(newitem);
                view.Reload();
            };

            return addItem;
        }
    }
}