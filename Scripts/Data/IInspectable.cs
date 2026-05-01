public interface IInspectable
{
	string InspectableName { get; }
	bool IsSelectable { get; }

	void SetSelected(bool selected);
	string GetInspectionText();
}
