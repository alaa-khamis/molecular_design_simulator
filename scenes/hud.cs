using Godot;
using System;
using System.Collections.Generic;
using GodotAtom = AtomClass.GodotAtom;
using System.Linq; 
using System.Text;



public partial class hud : CanvasLayer
{
	private RichTextLabel chemFormula;
	private Label currElement;
	private ColorRect color;

	public override void _Ready()
	{
		chemFormula = GetNode<RichTextLabel>("HUDControl/ChemicalFormula");
		currElement = GetNode<Label>("HUDControl/CurrentElement");
		color = GetNode<ColorRect>("HUDControl/ColorRect");
	}

	public void UpdateChemicalFormula(List<GodotAtom> atoms)
	{
		chemFormula.Clear();

		if (atoms.Count == 0)
		{
			chemFormula.AddText("No atoms yet!");
			return;
		}

		var elementCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

		foreach (var atom in atoms)
		{
			if (elementCounts.ContainsKey(atom.ElementSymbol))
			{
				elementCounts[atom.ElementSymbol]++;
			}
			else
			{
				elementCounts[atom.ElementSymbol] = 1;
			}
		}

		// Hill order: C first, then H, then alphabetical
		AppendFormattedElement(elementCounts, "C");
		AppendFormattedElement(elementCounts, "H");

		foreach (var element in elementCounts.OrderBy(e => e.Key))
		{
			AppendFormattedElement(element.Key, element.Value);
		}
	}

	private void AppendFormattedElement(Dictionary<string, int> elementCounts, string element)
	{
		if (elementCounts.ContainsKey(element))
		{
			chemFormula.AddText(element);
			if (elementCounts[element] > 1)
			{
				chemFormula.PushFontSize((int)(chemFormula.GetThemeFontSize("normal_font_size") * 0.75f));
				chemFormula.AddText(elementCounts[element].ToString());
				chemFormula.Pop();
			}
			elementCounts.Remove(element);
		}
	}

	private void AppendFormattedElement(string element, int count)
	{
		chemFormula.AddText(element);
		if (count > 1)
		{
			chemFormula.PushFontSize((int)(chemFormula.GetThemeFontSize("normal_font_size") * 0.75f));
			chemFormula.AddText(count.ToString());
			chemFormula.Pop();
		}
	}

	public void UpdateElementName(string name)
	{
		currElement.Text = name;
	}

	public void UpdateElementColor(Color x)
	{
		color.Color = x;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
