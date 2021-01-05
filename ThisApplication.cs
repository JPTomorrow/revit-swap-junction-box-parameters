/*
 * Created by SharpDevelop.
 * User: jmorrow
 * Date: 9/17/2018
 * Time: 6:28 AM
 *
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using System.Collections.Generic;
using System.Linq;
using JPMorrow.Revit.Documents;

namespace MainApp
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.DB.Macros.AddInId("58F7B2B7-BF6D-4B39-BBF8-13F7D9AAE97E")]
	public partial class ThisApplication : IExternalCommand
	{
		public Result Execute(ExternalCommandData cData, ref string message, ElementSet elements)
        {
			var dataDirectories = new string[0];
			var debugApp = false;

			//set revit documents
			var revit_info = ModelInfo.StoreDocuments(cData, dataDirectories, debugApp);

			List<ElementId> jboxes = revit_info.UIDOC.Selection.GetElementIds().ToList();

			//fail if no boxes selected
			if(!jboxes.Any())
			{
				RevitCustom.RevitCustomDebugger.Show(
					header:	"Junction Boxes",
					sub:	"None Selected",
					err:	"No junction boxes were selected. " +
							"please select them before running the script.");
				return Result.Succeeded;
			}

			List<ElementId> boxesFiltered = new List<ElementId>();
			foreach(ElementId id in jboxes)
			{
				Element box = revit_info.DOC.GetElement(id);
				if (box.Category.Name != "Electrical Fixtures") continue;
				if (String.IsNullOrWhiteSpace(box.LookupParameter("From").AsString()) ||
					String.IsNullOrWhiteSpace(box.LookupParameter("To").AsString())) continue;
				boxesFiltered.Add(id);
			}

			//check for parameters and quit if null
			Element testBox = revit_info.DOC.GetElement(boxesFiltered.First());
			if (testBox.LookupParameter("From") == null &&
				testBox.LookupParameter("To") == null)
			{
				RevitCustom.RevitCustomDebugger.Show(
					header: "Junction Boxes",
					sub: "Parameters",
					err: "You do not have the 'To' or 'From' parameters " +
							"loaded for electrical fixtures. These parameters " +
							"are required in order for this program to run.");
				return Result.Succeeded;
			}

			int successCnt = 0;
			using (TransactionGroup tgx = new TransactionGroup(revit_info.DOC, "Swapping Parameters"))
			{
				tgx.Start();
				foreach(ElementId id in boxesFiltered)
				{
					using (Transaction tx = new Transaction(revit_info.DOC, "swapping parameters"))
					{
						tx.Start();
						Element box = revit_info.DOC.GetElement(id);
						string From = box.LookupParameter("From").AsString();
						string To = box.LookupParameter("To").AsString();
						box.LookupParameter("From").Set(To);
						box.LookupParameter("To").Set(From);
						tx.Commit();
					}
					successCnt++;

				}
				tgx.Assimilate();
			}

			RevitCustom.RevitCustomDebugger.Show(	header:"JBox To/From Swap",
													sub:"Results",
													err:successCnt.ToString() + " junction boxes had their parameters swapped.");
			return Result.Succeeded;
        }

		#region startup
		private void Module_Startup(object sender, EventArgs e)
		{

		}

		private void Module_Shutdown(object sender, EventArgs e)
		{

		}
		#endregion

		#region Revit Macros generated code
		private void InternalStartup()
		{
			this.Startup += new System.EventHandler(Module_Startup);
			this.Shutdown += new System.EventHandler(Module_Shutdown);
		}
		#endregion
	}
}