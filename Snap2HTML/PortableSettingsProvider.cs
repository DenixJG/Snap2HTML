// Source: http://www.codeproject.com/Articles/20917/Creating-a-Custom-Settings-Provider , License: The Code Project Open License (CPOL)
// To use: For each setting in properties: Properties->Provider set to PortableSettingsProvider
// If this does not compile: Project->Add Reference->.Net-> Doubleclick "System.Configuration"

using System.Collections;
using System.Collections.Specialized;
using System.Configuration;
using System.Windows.Forms;
using System.Xml;

public class PortableSettingsProvider : SettingsProvider
{
    private const string SettingsRoot = "Settings";
    private XmlDocument? _settingsXml;

    public override void Initialize(string? name, NameValueCollection? col)
    {
        base.Initialize(ApplicationName, col);
    }

    public override string ApplicationName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Application.ProductName))
            {
                return Application.ProductName;
            }

            var fi = new FileInfo(Application.ExecutablePath);
            return fi.Name[..^fi.Extension.Length];
        }
        set { } // Do nothing
    }

    public override string Name => "PortableSettingsProvider";

    public virtual string GetAppSettingsPath()
    {
        var fi = new FileInfo(Application.ExecutablePath);
        return fi.DirectoryName ?? string.Empty;
    }

    public virtual string GetAppSettingsFilename() => ApplicationName + ".settings";

    public override void SetPropertyValues(SettingsContext context, SettingsPropertyValueCollection propvals)
    {
        // Iterate through the settings to be stored
        // Only dirty settings are included in propvals, and only ones relevant to this provider
        foreach (SettingsPropertyValue propval in propvals)
        {
            SetValue(propval);
        }

        try
        {
            SettingsXml.Save(Path.Combine(GetAppSettingsPath(), GetAppSettingsFilename()));
        }
        catch
        {
            // Ignore if can't save, device been ejected
        }
    }

    public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext context, SettingsPropertyCollection props)
    {
        // Create new collection of values
        var values = new SettingsPropertyValueCollection();

        // Iterate through the settings to be retrieved
        foreach (SettingsProperty setting in props)
        {
            var value = new SettingsPropertyValue(setting)
            {
                IsDirty = false,
                SerializedValue = GetValue(setting)
            };
            values.Add(value);
        }

        return values;
    }

    private XmlDocument SettingsXml
    {
        get
        {
            // If we don't hold an xml document, try opening one.  
            // If it doesn't exist then create a new one ready.
            if (_settingsXml == null)
            {
                _settingsXml = new XmlDocument();

                try
                {
                    _settingsXml.Load(Path.Combine(GetAppSettingsPath(), GetAppSettingsFilename()));
                }
                catch
                {
                    // Create new document
                    var dec = _settingsXml.CreateXmlDeclaration("1.0", "utf-8", string.Empty);
                    _settingsXml.AppendChild(dec);

                    var nodeRoot = _settingsXml.CreateNode(XmlNodeType.Element, SettingsRoot, "");
                    _settingsXml.AppendChild(nodeRoot);
                }
            }

            return _settingsXml;
        }
    }

    private string GetValue(SettingsProperty setting)
    {
        try
        {
            var xpath = IsRoaming(setting)
                ? $"{SettingsRoot}/{setting.Name}"
                : $"{SettingsRoot}/{Environment.MachineName}/{setting.Name}";

            return SettingsXml.SelectSingleNode(xpath)?.InnerText ?? setting.DefaultValue?.ToString() ?? string.Empty;
        }
        catch
        {
            return setting.DefaultValue?.ToString() ?? string.Empty;
        }
    }

    private void SetValue(SettingsPropertyValue propVal)
    {
        XmlElement? settingNode = null;

        // Determine if the setting is roaming.
        // If roaming then the value is stored as an element under the root
        // Otherwise it is stored under a machine name node 
        try
        {
            var xpath = IsRoaming(propVal.Property)
                ? $"{SettingsRoot}/{propVal.Name}"
                : $"{SettingsRoot}/{Environment.MachineName}/{propVal.Name}";

            settingNode = (XmlElement?)SettingsXml.SelectSingleNode(xpath);
        }
        catch
        {
            settingNode = null;
        }

        // Check to see if the node exists, if so then set its new value
        if (settingNode != null)
        {
            settingNode.InnerText = propVal.SerializedValue?.ToString() ?? string.Empty;
        }
        else if (IsRoaming(propVal.Property))
        {
            // Store the value as an element of the Settings Root Node
            settingNode = SettingsXml.CreateElement(propVal.Name);
            settingNode.InnerText = propVal.SerializedValue?.ToString() ?? string.Empty;
            SettingsXml.SelectSingleNode(SettingsRoot)?.AppendChild(settingNode);
        }
        else
        {
            // It's machine specific, store as an element of the machine name node,
            // creating a new machine name node if one doesn't exist.
            XmlElement? machineNode;
            try
            {
                machineNode = (XmlElement?)SettingsXml.SelectSingleNode($"{SettingsRoot}/{Environment.MachineName}");
            }
            catch
            {
                machineNode = null;
            }

            if (machineNode == null)
            {
                machineNode = SettingsXml.CreateElement(Environment.MachineName);
                SettingsXml.SelectSingleNode(SettingsRoot)?.AppendChild(machineNode);
            }

            settingNode = SettingsXml.CreateElement(propVal.Name);
            settingNode.InnerText = propVal.SerializedValue?.ToString() ?? string.Empty;
            machineNode.AppendChild(settingNode);
        }
    }

    private static bool IsRoaming(SettingsProperty prop)
    {
        // Determine if the setting is marked as Roaming
        foreach (DictionaryEntry d in prop.Attributes)
        {
            if (d.Value is SettingsManageabilityAttribute)
            {
                return true;
            }
        }

        return false;
    }
}
