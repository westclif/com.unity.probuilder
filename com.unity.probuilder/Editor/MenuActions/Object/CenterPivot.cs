using UnityEngine.ProBuilder;
using UnityEngine;

namespace UnityEditor.ProBuilder.Actions
{
	sealed class CenterPivot : MenuAction
	{
		public override ToolbarGroup group
		{
			get { return ToolbarGroup.Object; }
		}

		public override Texture2D icon
		{
			get { return IconUtility.GetIcon("Toolbar/Pivot_CenterOnObject", IconSkin.Pro); }
		}

		public override TooltipContent tooltip
		{
			get { return _tooltip; }
		}

		static readonly TooltipContent _tooltip = new TooltipContent
		(
			"Center Pivot",
			@"Set the pivot point of this object to the center of it's bounds."
		);

		public override bool IsEnabled()
		{
			return ProBuilderEditor.instance != null && MeshSelection.TopInternal().Length > 0;
		}

		public override ActionResult DoAction()
		{
			return MenuCommands.MenuCenterPivot(MeshSelection.TopInternal());
		}
	}
}