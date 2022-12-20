namespace SaveFile;

public class SaveEntry
{
	public SaveEntry(string key, object value)
	{
		this.key = key;
		this.value = value;
	}

	public string key { get; set; }
	public object value { get; set; }
}