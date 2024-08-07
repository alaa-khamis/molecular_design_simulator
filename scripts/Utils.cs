using Godot;
using Classes;
using System.Collections.Generic;
using System;

public class Utils
{
	public static Godot.Vector3 ConvertToGodotVector3(System.Numerics.Vector3 vector)
	{
		return new Godot.Vector3(vector.X, vector.Y, vector.Z);
	}

	public static System.Numerics.Vector3 ConvertToNumericsVector3(Godot.Vector3 vector)
	{
		return new System.Numerics.Vector3(vector.X, vector.Y, vector.Z);
	}

	public static List<AtomBase> LoadElementsFromJSON()
	{

		var elements = new List<AtomBase>();
		string jsonText, filePath = "res://assets/periodic_table.json";

		using (var file = Godot.FileAccess.Open(filePath, Godot.FileAccess.ModeFlags.Read))
		{
			if (file == null)
			{
				GD.PrintErr("Failed to open periodic_table.json");
				return elements;
			}
			jsonText = file.GetAsText();
		}

		var jsonData = Json.ParseString(jsonText);

		if (jsonData.VariantType != Variant.Type.Nil)
		{
			var rootDictionary = jsonData.AsGodotDictionary();
			var elementsArray = rootDictionary["elements"].AsGodotArray();


			foreach (var element in elementsArray)
			{
				var elementData = element.AsGodotDictionary();
				var atomColorArray = (Godot.Collections.Array)elementData["atomColor"];

				var atomColor = new System.Numerics.Vector3(
					(float)atomColorArray[0] / 255.0f,
					(float)atomColorArray[1] / 255.0f,
					(float)atomColorArray[2] / 255.0f
				);

				var atom = new AtomBase
				{
					ElementSymbol = (string)elementData["ElementSymbol"],
					ElementName = (string)elementData["ElementName"],
					AtomicNumber = (int)elementData["AtomicNumber"],
					Mass = (float)elementData["Mass"],
					CovalentRadius = (float)elementData["CovalentRadius"],
					VanDerWaalsRadius = (float)elementData["VanDerWaalsRadius"],
					AtomColor = atomColor
				};
				elements.Add(atom);
			}
		}
		else
		{
			GD.PrintErr("Failed to parse elements.json");
		}

		return elements;
	}

	public static string GetCurrentDirectory()
	{
		// Get the absolute path to the executable
		string executablePath = OS.GetExecutablePath();

		// Get the directory of the executable
		string executableDir = System.IO.Path.GetDirectoryName(executablePath);

		// Convert to a relative path (from project root)
		string projectPath = ProjectSettings.GlobalizePath("res://");
		string relativePath = executableDir.Replace(projectPath, "");

		// Return the relative path
		return relativePath;
	}
}

