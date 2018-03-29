namespace Sitecore.Support.Speak.Components.Models.ListsAndGrids.Grids
{
  using Newtonsoft.Json;
  using Sitecore;
  using Sitecore.Data.Items;
  using Sitecore.Mvc.Presentation;
  using Sitecore.Speak.Components.Models;
  using System;
  using System.Collections.Generic;
  using System.Globalization;
  using System.Linq;
  using System.Runtime.CompilerServices;
  using Sitecore.Speak.Components.Models.ListsAndGrids.Grids;

  public class GridRenderingModel : ComponentRenderingModel
  {
    public Sitecore.Speak.Components.Models.ListsAndGrids.Grids.GridAttributes GridAttributes;
    private Guid LayoutTypeId;
    private int MaxColumnNumber = 12;
    private const int SizesCount = 4;
    private int TotalNumberOfCells;

    private int ConvertSize(int size)
    {
      int maxColumnNumber = this.MaxColumnNumber / size;
      if (maxColumnNumber <= 0)
      {
        maxColumnNumber = 1;
      }
      if (maxColumnNumber > this.MaxColumnNumber)
      {
        maxColumnNumber = this.MaxColumnNumber;
      }
      return maxColumnNumber;
    }

    private void InitialiseCells()
    {
      try
      {
        this.GridAttributes = JsonConvert.DeserializeObject<Sitecore.Speak.Components.Models.ListsAndGrids.Grids.GridAttributes>(this.CellAttributes);
        this.ValidateCellsData();
        this.SetRowLastCells();
      }
      #region Bug 214239 Fix
      catch
      {
        this.ResetGridAttributes();
        this.SetRowLastCells();
      }
      #endregion
    }

    public override void Initialize(Rendering rendering)
    {
      base.Initialize(rendering);
      this.LayoutType = this.GetString("LayoutType", "");
      if (this.NumberOfCells > 0)
      {
        this.TotalNumberOfCells = this.NumberOfCells;
        this.InitializeColumns();
      }
      this.PaddingClass = this.UseCellBottomPadding ? "sc-bottom-padding" : string.Empty;
    }

    private void InitializeColumns()
    {
      this.NumberOfColumsPerSize = new List<int>(4);
      this.NumberOfColumsPerSize.Add(1);
      this.NumberOfColumsPerSize.Add(1);
      this.NumberOfColumsPerSize.Add(1);
      this.NumberOfColumsPerSize.Add(1);
      Item item = null;
      if (!string.IsNullOrEmpty(this.LayoutType))
      {
        item = ClientHost.Databases.Database.GetItem(this.LayoutType);
      }
      if (item != null)
      {
        this.LayoutTypeId = item.ID.ToGuid();
        this.NumberOfColumsPerSize[0] = System.Convert.ToInt32(item["Large"], CultureInfo.InvariantCulture);
        this.NumberOfColumsPerSize[1] = System.Convert.ToInt32(item["Medium"], CultureInfo.InvariantCulture);
        this.NumberOfColumsPerSize[2] = System.Convert.ToInt32(item["Small"], CultureInfo.InvariantCulture);
        this.NumberOfColumsPerSize[3] = System.Convert.ToInt32(item["X-Small"], CultureInfo.InvariantCulture);
      }
      this.GridCellSizes = new List<int>(4);
      this.GridCellSizes.Add(this.ConvertSize(this.NumberOfColumsPerSize[0]));
      this.GridCellSizes.Add(this.ConvertSize(this.NumberOfColumsPerSize[1]));
      this.GridCellSizes.Add(this.ConvertSize(this.NumberOfColumsPerSize[2]));
      this.GridCellSizes.Add(this.ConvertSize(this.NumberOfColumsPerSize[3]));
      this.InitialiseCells();
    }

    public void InitializeColumns(int numberOfCells)
    {
      this.TotalNumberOfCells = numberOfCells;
      this.InitializeColumns();
    }

    private void ResetGridAttributes()
    {
      this.GridAttributes = new Sitecore.Speak.Components.Models.ListsAndGrids.Grids.GridAttributes();
      List<SizeAttributes> list1 = new List<SizeAttributes>();
      SizeAttributes item = new SizeAttributes
      {
        Size = "Large",
        CellAttributes = new List<Sitecore.Speak.Components.Models.ListsAndGrids.Grids.CellAttributes>(),
        NumberOfColumns = this.NumberOfColumsPerSize[0]
      };
      list1.Add(item);
      SizeAttributes attributes2 = new SizeAttributes
      {
        Size = "Medium",
        CellAttributes = new List<Sitecore.Speak.Components.Models.ListsAndGrids.Grids.CellAttributes>(),
        NumberOfColumns = this.NumberOfColumsPerSize[1]
      };
      list1.Add(attributes2);
      SizeAttributes attributes3 = new SizeAttributes
      {
        Size = "Small",
        CellAttributes = new List<Sitecore.Speak.Components.Models.ListsAndGrids.Grids.CellAttributes>(),
        NumberOfColumns = this.NumberOfColumsPerSize[2]
      };
      list1.Add(attributes3);
      SizeAttributes attributes4 = new SizeAttributes
      {
        Size = "XSmall",
        CellAttributes = new List<Sitecore.Speak.Components.Models.ListsAndGrids.Grids.CellAttributes>(),
        NumberOfColumns = this.NumberOfColumsPerSize[3]
      };
      list1.Add(attributes4);
      this.GridAttributes.SizeAttributes = list1;
      for (int i = 0; i < 4; i++)
      {
        for (int j = 0; j < this.TotalNumberOfCells; j++)
        {
          Sitecore.Speak.Components.Models.ListsAndGrids.Grids.CellAttributes attributes5 = new Sitecore.Speak.Components.Models.ListsAndGrids.Grids.CellAttributes
          {
            Index = j + 1,
            Span = 1,
            IsLastInRow = ((j + 1) % this.NumberOfColumsPerSize[i]) == 0
          };
          this.GridAttributes.SizeAttributes[i].CellAttributes.Add(attributes5);
        }
      }
    }

    private void SetRowLastCells()
    {
      int num = 0;
      foreach (SizeAttributes attributes in this.GridAttributes.SizeAttributes)
      {
        int num2 = 0;
        int num3 = 0;
        foreach (Sitecore.Speak.Components.Models.ListsAndGrids.Grids.CellAttributes attributes2 in attributes.CellAttributes)
        {
          num2 += attributes2.Span;
          if (num2 > this.NumberOfColumsPerSize[num])
          {
            attributes2.IsLastInRow = true;
            num2 = 0;
          }
          else if (attributes.CellAttributes.Last<Sitecore.Speak.Components.Models.ListsAndGrids.Grids.CellAttributes>().Equals(attributes2))
          {
            attributes2.IsLastInRow = true;
            num2 = 0;
          }
          else if ((num2 + attributes.CellAttributes[num3 + 1].Span) > this.NumberOfColumsPerSize[num])
          {
            attributes2.IsLastInRow = true;
            num2 = 0;
          }
          num3++;
        }
        num++;
      }
    }

    private void ValidateCellsData()
    {
      if (((this.GridAttributes == null) || (this.GridAttributes.NumberOfCells < 1)) || ((string.IsNullOrEmpty(this.GridAttributes.LayoutType) || (this.GridAttributes.SizeAttributes == null)) || !this.GridAttributes.SizeAttributes.Any<SizeAttributes>()))
      {
        this.ResetGridAttributes();
      }
      else if (this.TotalNumberOfCells != this.GridAttributes.NumberOfCells)
      {
        this.ResetGridAttributes();
      }
      else if (!this.LayoutTypeId.Equals(new Guid(this.GridAttributes.LayoutType)))
      {
        this.ResetGridAttributes();
      }
      else
      {
        int count = -1;
        int num2 = 0;
        foreach (SizeAttributes attributes in this.GridAttributes.SizeAttributes)
        {
          if (attributes == null)
          {
            this.ResetGridAttributes();
            break;
          }
          if (count == -1)
          {
            count = this.GridAttributes.SizeAttributes[0].CellAttributes.Count;
          }
          else if (this.GridAttributes.SizeAttributes[0].CellAttributes.Count != count)
          {
            this.ResetGridAttributes();
            break;
          }
          if (attributes.NumberOfColumns != this.NumberOfColumsPerSize[num2])
          {
            this.ResetGridAttributes();
            break;
          }
          num2++;
        }
        if (this.TotalNumberOfCells != count)
        {
          this.ResetGridAttributes();
        }
      }
    }

    public string CellAttributes =>
        this.GetString("CellAttributes", "");

    public List<int> GridCellSizes { get; set; }

    public string Id =>
        this.GetString("Id", "");

    public bool IsVisible =>
        this.GetBool("IsVisible", false);

    public string LayoutType { get; set; }

    public int NumberOfCells =>
        this.GetInt("NumberOfCells", 0);

    private List<int> NumberOfColumsPerSize { get; set; }

    public string PaddingClass { get; set; }

    public bool UseCellBottomPadding =>
        this.GetBool("UseCellBottomPadding", false);
  }
}
