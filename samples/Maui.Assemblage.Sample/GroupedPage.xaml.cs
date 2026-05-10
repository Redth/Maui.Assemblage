using Maui.Assemblage.Core.Data;

namespace Maui.Assemblage.Sample;

public partial class GroupedPage : ContentPage
{
	public GroupedPage()
	{
		InitializeComponent();

		var fruits = new Dictionary<string, string[]>
		{
			["A"] = ["Apple", "Apricot", "Avocado"],
			["B"] = ["Banana", "Blueberry", "Blackberry"],
			["C"] = ["Cherry", "Coconut", "Cranberry"],
			["D"] = ["Date", "Dragonfruit", "Durian"],
			["G"] = ["Grape", "Grapefruit", "Guava"],
			["K"] = ["Kiwi", "Kumquat"],
			["L"] = ["Lemon", "Lime", "Lychee"],
			["M"] = ["Mango", "Melon", "Mulberry"],
			["O"] = ["Orange", "Olive"],
			["P"] = ["Papaya", "Peach", "Pear", "Pineapple", "Plum", "Pomegranate"],
			["R"] = ["Raspberry"],
			["S"] = ["Strawberry", "Starfruit"],
			["W"] = ["Watermelon"],
		};

		var sections = fruits.Select(kvp =>
			new GroupSection(kvp.Key, kvp.Value.Cast<object?>().ToArray())).ToList();

		GroupedList.DataSource = new GroupedCollectionDataSource(sections);
	}
}
