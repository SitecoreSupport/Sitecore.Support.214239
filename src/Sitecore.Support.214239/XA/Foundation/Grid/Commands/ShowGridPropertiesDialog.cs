// Sitecore.XA.Foundation.Grid.Commands.ShowGridPropertiesDialog
using Microsoft.Extensions.DependencyInjection;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.DependencyInjection;
using Sitecore.Globalization;
using Sitecore.Layouts;
using Sitecore.Pipelines;
using Sitecore.Shell.Applications.ContentEditor;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.Sheer;
using Sitecore.XA.Foundation.Abstractions.Wrappers;
using Sitecore.XA.Foundation.Grid;
using Sitecore.XA.Foundation.Grid.Model;
using Sitecore.XA.Foundation.Grid.Pipelines.GetGridParametersDialogFields;
using Sitecore.XA.Foundation.SitecoreExtensions.Extensions;
using Sitecore.XA.Foundation.SitecoreExtensions.Repositories;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Sitecore.XA.Foundation.Grid.Commands;

namespace Sitecore.Support.XA.Foundation.Grid.Commands
{

  public class ShowGridPropertiesDialog : WebEditCommandWrapper
  {
    public override CommandState QueryState(CommandContext context)
    {
      if (!string.IsNullOrEmpty(context.Parameters["placeHolderKey"]))
      {
        return CommandState.Enabled;
      }
      string value = context.Parameters["renderingId"];
      if (string.IsNullOrEmpty(value))
      {
        return CommandState.Hidden;
      }
      Item item = ServiceLocator.ServiceProvider.GetService<IContentRepository>().GetItem(ID.Parse(value));
      if (item == null)
      {
        return CommandState.Hidden;
      }
      if (RenderingItem.GetStandardValuesItemFromParametersTemplate(item).InheritsFrom(Templates.GridParameters.ID))
      {
        return CommandState.Enabled;
      }
      return CommandState.Hidden;
    }

    public override void Execute(CommandContext context)
    {
      base.Context.ClientPage.Start(this, "Run", Parameters(context));
    }

    protected virtual void UpdateLayout(NameValueCollection contextParameters, FieldEditorOptions fieldEditorOptions)
    {
      string placeholder = contextParameters["placeHolderKey"];
      string uniqueId = Guid.Parse(contextParameters["renderingUid"]).ToString("B").ToUpperInvariant();
      string text = contextParameters["fieldName"];
      LayoutDefinition layoutDefinition = GetLayoutDefinition();
      if (layoutDefinition == null)
      {
        ReturnLayout();
        return;
      }
      string id = ShortID.Decode(WebUtil.GetFormValue("scDeviceID"));
      DeviceDefinition device = layoutDefinition.GetDevice(id);
      if (device == null)
      {
        ReturnLayout();
        return;
      }
      RenderingDefinition renderingByUniqueId = device.GetRenderingByUniqueId(uniqueId);
      if (renderingByUniqueId == null)
      {
        ReturnLayout();
        return;
      }
      if (string.IsNullOrEmpty(renderingByUniqueId.Parameters))
      {
        if (!string.IsNullOrEmpty(renderingByUniqueId.ItemID))
        {
          RenderingItem renderingItem = Client.ContentDatabase.GetItem(renderingByUniqueId.ItemID);
          renderingByUniqueId.Parameters = ((renderingItem != null) ? renderingItem.Parameters : string.Empty);
        }
        else
        {
          renderingByUniqueId.Parameters = string.Empty;
        }
      }
      NameValueCollection nameValueCollection = WebUtil.ParseUrlParameters(renderingByUniqueId.Parameters);
      foreach (FieldDescriptor field in fieldEditorOptions.Fields)
      {
        Item item = ServiceLocator.ServiceProvider.GetService<IContentRepository>().GetItem(field.FieldID);
        if (text == item.Name)
        {
          FillGridParameters(contextParameters, device, nameValueCollection, text, field.Value);
        }
        else
        {
          nameValueCollection[item.Name] = field.Value;
        }
      }
      renderingByUniqueId.Parameters = new UrlString(nameValueCollection.EscapeDataValues()).GetUrl();
      string layout = WebEditUtil.ConvertXMLLayoutToJSON(layoutDefinition.ToXml());
      ReturnLayout(layout, renderingByUniqueId.UniqueId, placeholder);
    }

    protected virtual void Run(ClientPipelineArgs args)
    {
      if (!args.IsPostBack)
      {
        ShowFieldEditorDialog(args.Parameters);
        args.WaitForPostBack();
      }
      else if (!string.IsNullOrEmpty(args.Result) && args.Result.ToLowerInvariant() != "undefined")
      {
        FieldEditorOptions fieldEditorOptions = FieldEditorOptions.Parse(args.Result);
        UpdateLayout(args.Parameters, fieldEditorOptions);
      }
    }

    protected virtual void ShowFieldEditorDialog(NameValueCollection parameters)
    {
      Item item = ServiceLocator.ServiceProvider.GetService<IContentRepository>().GetItem(new ID(parameters["itemId"]));
      ID id = new ID(parameters["renderingId"]);
      Item item2 = ServiceLocator.ServiceProvider.GetService<IContentRepository>().GetItem(id);
      Item standardValuesItemFromParametersTemplate = RenderingItem.GetStandardValuesItemFromParametersTemplate(item2);
      GridParametersDialogFieldsArgs gridParametersDialogFieldsArgs = new GridParametersDialogFieldsArgs
      {
        Parameters = parameters,
        StandardValuesItemFromParametersTemplate = standardValuesItemFromParametersTemplate,
        RenderingItem = item2,
        RenderingDefinition = GetContextRendering(parameters["renderingUid"])
      };
      CorePipeline.Run("getGridParametersDialogFields", gridParametersDialogFieldsArgs);
      FieldEditorOptions fieldEditorOptions = new FieldEditorOptions(gridParametersDialogFieldsArgs.Fields);
      fieldEditorOptions.PreserveSections = true;
      fieldEditorOptions.SaveItem = false;
      fieldEditorOptions.IsInFrame = true;
      fieldEditorOptions.DialogTitle = Translate.Text("Set the grid details.");
      fieldEditorOptions.Title = Translate.Text("Set the grid details.");
      fieldEditorOptions.Parameters["contentitem"] = item.Uri.ToString();
      fieldEditorOptions.Parameters["rendering"] = new ItemUri(item2).ToString();
      UrlString urlString = fieldEditorOptions.ToUrlString();
      urlString["sc_content"] = "master";
      SheerResponse.ShowModalDialog(new ModalDialogOptions(urlString.ToString())
      {
        Response = true,
        Width = "750px"
      });
    }

    protected virtual NameValueCollection Parameters(CommandContext context)
    {
      NameValueCollection nameValueCollection = HttpUtility.ParseQueryString("");
      string text = string.IsNullOrWhiteSpace(context.Parameters["fieldName"]) ? "GridParameters" : context.Parameters["fieldName"];
      string text2 = context.Parameters["referenceId"];
      RenderingDefinition contextRendering = GetContextRendering(text2);
      nameValueCollection["renderingUid"] = text2;
      nameValueCollection["fieldName"] = text;
      nameValueCollection["values"] = GetGridParametersValues(contextRendering, text);
      nameValueCollection["itemId"] = context.Items.First().ID.ToString();
      nameValueCollection["deviceid"] = ShortID.Decode(WebUtil.GetFormValue("scDeviceID"));
      nameValueCollection["renderingId"] = contextRendering.ItemID;
      return nameValueCollection;
    }

    protected virtual RenderingDefinition GetContextRendering(string renderingUid)
    {
      LayoutDefinition layoutDefinition = GetLayoutDefinition();
      if (layoutDefinition == null)
      {
        return null;
      }
      string id = ShortID.Decode(WebUtil.GetFormValue("scDeviceID"));
      DeviceDefinition device = layoutDefinition.GetDevice(id);
      if (device == null)
      {
        return null;
      }
      string uniqueId = Guid.Parse(renderingUid).ToString("B").ToUpperInvariant();
      return device.GetRenderingByUniqueId(uniqueId);
    }

    protected virtual LayoutDefinition GetLayoutDefinition()
    {
      return LayoutDefinition.Parse(WebEditUtil.ConvertJSONLayoutToXML(WebUtil.GetFormValue("scLayout")));
    }

    protected virtual string GetGridParametersValues(RenderingDefinition rendering, string fieldName)
    {
      return WebUtil.ParseUrlParameters(rendering.Parameters ?? string.Empty)[fieldName];
    }

    protected virtual void FillGridParameters(NameValueCollection contextParameters, DeviceDefinition device, NameValueCollection parameters, string fieldName, string value)
    {
      Item item = ServiceLocator.ServiceProvider.GetService<IContentRepository>().GetItem(contextParameters["itemId"]);
      Item gridDefinitionItem = ServiceLocator.ServiceProvider.GetService<IGridContext>().GetGridDefinitionItem(item, Client.ContentDatabase.GetItem(device.ID));
      #region SUPPORT PATCH #214239
      /* Value item ID can be encoded, if it is encoded, it throws an exception. 
       Patch checks if value is encoded with the following logic:
       Decode, compare to original. If it does differ, original is encoded. If it doesn't differ, original isn't encoded*/
      string[] values = value.Split(new char[1]{'|'}, StringSplitOptions.RemoveEmptyEntries);
      for(int i = 0;i < values.Length;i++)
      {
        string decoded = Uri.UnescapeDataString(values[i]);
        if(values[i] != decoded)
        {
          values[i] = decoded;
        }
      }
      List<ID> list = (from id in values.Select(ID.Parse)
                       where !id.IsNull
                       select id).ToList();
      #endregion

      if (list.Any())
      {
        parameters[fieldName] = new GridDefinition(gridDefinitionItem).InstantiateGridFieldParser().ToFieldValue(list);
      }
      else
      {
        parameters[fieldName] = string.Empty;
      }
    }

    protected virtual void ReturnLayout(string layout = null, string referenceId = null, string placeholder = null)
    {
      SheerResponse.SetAttribute("scLayoutDefinition", "value", layout ?? string.Empty);
      if (!string.IsNullOrEmpty(layout) && !string.IsNullOrEmpty(referenceId))
      {
        string str = "r_" + ID.Parse(referenceId).ToShortID();
        SheerResponse.Eval("window.parent.Sitecore.PageModes.ChromeManager.handleMessage('chrome:rendering:propertiescompleted', {controlId : '" + str + "'});");
        if (!string.IsNullOrEmpty(placeholder))
        {
          string str2 = Regex.Replace(placeholder, "\\W", "_");
          SheerResponse.Eval("function highlight() { if (window.parent.Sitecore.PageModes.ChromeHighlightManager._stopped) setTimeout(highlight, 100); else { window.parent.Sitecore.PageModes.ChromeManager.hideSelection(); window.parent.Sitecore.PageModes.ChromeManager.select(window.parent.$sc.first(window.parent.Sitecore.PageModes.ChromeManager.chromes(), function() { return this.controlId() == '" + str2 + "'; })); }; }; setTimeout(highlight, 100);");
        }
      }
    }
  }

}