﻿using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Xsl;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using Waher.Content.Xml;
using Waher.Content.Xsl;
using Waher.Networking.XMPP.Sensor;
using Waher.Things;
using Waher.Things.SensorData;
using Waher.Client.WPF.Model;
using Waher.Client.WPF.Controls.SensorData;

namespace Waher.Client.WPF.Controls
{
	/// <summary>
	/// Interaction logic for SensorDataView.xaml
	/// </summary>
	public partial class SensorDataView : UserControl, ITabView
	{
		private SensorDataClientRequest request;
		private TreeNode node;
		private Dictionary<string, bool> nodes = new Dictionary<string, bool>();
		private Dictionary<string, bool> failed = new Dictionary<string, bool>();

		public SensorDataView(SensorDataClientRequest Request, TreeNode Node)
		{
			this.request = Request;
			this.node = Node;

			InitializeComponent();

			if (this.request != null)
			{
				this.request.OnStateChanged += new SensorDataReadoutStateChangedEventHandler(Request_OnStateChanged);
				this.request.OnFieldsReceived += new SensorDataReadoutFieldsReportedEventHandler(Request_OnFieldsReceived);
				this.request.OnErrorsReceived += new SensorDataReadoutErrorsReportedEventHandler(Request_OnErrorsReceived);
			}
		}

		public void Dispose()
		{
			if (this.request != null)
			{
				this.request.OnStateChanged -= new SensorDataReadoutStateChangedEventHandler(Request_OnStateChanged);
				this.request.OnFieldsReceived -= new SensorDataReadoutFieldsReportedEventHandler(Request_OnFieldsReceived);
				this.request.OnErrorsReceived -= new SensorDataReadoutErrorsReportedEventHandler(Request_OnErrorsReceived);
			}

			if (this.SensorDataListView.View is GridView GridView)
			{
				Registry.SetValue(MainWindow.registryKey, "SensorDataTimestampWidth", (int)GridView.Columns[0].Width, RegistryValueKind.DWord);
				Registry.SetValue(MainWindow.registryKey, "SensorDataFieldWidth", (int)GridView.Columns[1].Width, RegistryValueKind.DWord);
				Registry.SetValue(MainWindow.registryKey, "SensorDataValueWidth", (int)GridView.Columns[2].Width, RegistryValueKind.DWord);
				Registry.SetValue(MainWindow.registryKey, "SensorDataUnitWidth", (int)GridView.Columns[3].Width, RegistryValueKind.DWord);
				Registry.SetValue(MainWindow.registryKey, "SensorDataStatusWidth", (int)GridView.Columns[4].Width, RegistryValueKind.DWord);
				Registry.SetValue(MainWindow.registryKey, "SensorDataTypeWidth", (int)GridView.Columns[5].Width, RegistryValueKind.DWord);
			}
		}

		public TreeNode Node
		{
			get { return this.node; }
		}

		private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (this.SensorDataListView.View is GridView GridView)
			{
				GridView.Columns[2].Width = this.ActualWidth - GridView.Columns[0].ActualWidth - GridView.Columns[1].ActualWidth -
					GridView.Columns[3].ActualWidth - GridView.Columns[4].ActualWidth - GridView.Columns[5].ActualWidth -
					SystemParameters.VerticalScrollBarWidth - 8;
			}
		}

		private void Request_OnErrorsReceived(object Sender, IEnumerable<ThingError> NewErrors)
		{
			this.Dispatcher.BeginInvoke(new ParameterizedThreadStart(this.OnErrorsReceived), NewErrors);
		}

		private void OnErrorsReceived(object P)
		{
			IEnumerable<ThingError> NewErrors = (IEnumerable<ThingError>)P;
			string Key;

			lock (this.failed)
			{
				foreach (ThingError Error in NewErrors)
				{
					Key = Error.Key;
					this.failed[Key] = true;
					this.nodes[Key] = true;
				}

				this.NodesFailedLabel.Content = this.failed.Count.ToString();
				this.NodesTotalLabel.Content = this.nodes.Count.ToString();
			}

			// TODO: Display errors.
		}

		private void Request_OnFieldsReceived(object Sender, IEnumerable<Field> NewFields)
		{
			this.Dispatcher.BeginInvoke(new ParameterizedThreadStart(this.OnFieldsReceived), NewFields);
		}

		private void OnFieldsReceived(object P)
		{
			IEnumerable<Field> NewFields = (IEnumerable<Field>)P;
			string LastKey = null;
			string Key;

			lock (this.nodes)
			{
				foreach (Field Field in NewFields)
				{
					Key = Field.Thing.Key;
					if (LastKey == null || Key != LastKey)
					{
						LastKey = Key;
						this.nodes[Key] = true;
					}

					this.SensorDataListView.Items.Add(new FieldItem(Field));
				}

				this.NodesTotalLabel.Content = this.nodes.Count.ToString();
				this.FieldsLabel.Content = this.SensorDataListView.Items.Count.ToString();
			}
		}

		private void Request_OnStateChanged(object Sender, SensorDataReadoutState NewState)
		{
			this.Dispatcher.BeginInvoke(new ParameterizedThreadStart(this.OnStateChanged), NewState);
		}

		private void OnStateChanged(object P)
		{
			SensorDataReadoutState NewState = (SensorDataReadoutState)P;

			switch (NewState)
			{
				case SensorDataReadoutState.Requested:
					this.StateLabel.Content = "Requested";
					break;

				case SensorDataReadoutState.Accepted:
					this.StateLabel.Content = "Accepted";
					break;

				case SensorDataReadoutState.Started:
					this.StateLabel.Content = "Started";
					break;

				case SensorDataReadoutState.Receiving:
					this.StateLabel.Content = "Receiving";
					break;

				case SensorDataReadoutState.Done:
					this.StateLabel.Content = "Done";
					this.Done();
					break;

				case SensorDataReadoutState.Cancelled:
					this.StateLabel.Content = "Cancelled";
					break;

				case SensorDataReadoutState.Failure:
					this.StateLabel.Content = "Failure";
					this.Done();
					break;

				default:
					this.StateLabel.Content = NewState.ToString();
					break;
			}
		}

		private void Done()
		{
			lock (this.nodes)
			{
				this.NodesTotalLabel.Content = this.nodes.Count;
				this.NodesOkLabel.Content = this.nodes.Count - this.failed.Count;
			}
		}

		public void NewButton_Click(object sender, RoutedEventArgs e)
		{
			this.SensorDataListView.Items.Clear();
		}

		public void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			this.SaveAsButton_Click(sender, e);
		}

		public void SaveAsButton_Click(object sender, RoutedEventArgs e)
		{
			SaveFileDialog Dialog = new SaveFileDialog()
			{
				AddExtension = true,
				CheckPathExists = true,
				CreatePrompt = false,
				DefaultExt = "html",
				Filter = "XML Files (*.xml)|*.xml|HTML Files (*.html,*.htm)|*.html,*.htm|All Files (*.*)|*.*",
				Title = "Save Sensor data readout"
			};

			bool? Result = Dialog.ShowDialog(MainWindow.FindWindow(this));

			if (Result.HasValue && Result.Value)
			{
				try
				{
					if (Dialog.FilterIndex == 2)
					{
						StringBuilder Xml = new StringBuilder();
						using (XmlWriter w = XmlWriter.Create(Xml, XML.WriterSettings(true, true)))
						{
							this.SaveAsXml(w);
						}

						string Html = XSL.Transform(Xml.ToString(), sensorDataToHtml);

						File.WriteAllText(Dialog.FileName, Html, System.Text.Encoding.UTF8);
					}
					else
					{
						using (FileStream f = File.Create(Dialog.FileName))
						{
							using (XmlWriter w = XmlWriter.Create(f, XML.WriterSettings(true, false)))
							{
								this.SaveAsXml(w);
							}
						}
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show(MainWindow.FindWindow(this), ex.Message, "Unable to save file.", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		private static readonly XslCompiledTransform sensorDataToHtml = XSL.LoadTransform("Waher.Client.WPF.Transforms.SensorDataToHTML.xslt");
		private static readonly XmlSchema schema1 = XSL.LoadSchema("Waher.Client.WPF.Schema.SensorData.xsd");
		private static readonly XmlSchema schema2 = XSL.LoadSchema("Waher.Client.WPF.Schema.sensor-data.xsd");
		private const string sensorDataNamespace = "http://waher.se/Schema/SensorData.xsd";
		private const string sensorDataRoot = "SensorData";

		private void SaveAsXml(XmlWriter w)
		{
			List<Field> Fields = new List<Field>();

			foreach (FieldItem Item in this.SensorDataListView.Items)
				Fields.Add(Item.Field);

			w.WriteStartElement(sensorDataRoot, sensorDataNamespace);
			w.WriteAttributeString("state", this.StateLabel.Content.ToString());
			w.WriteAttributeString("nodesOk", this.NodesOkLabel.Content.ToString());
			w.WriteAttributeString("nodesFailed", this.NodesFailedLabel.Content.ToString());
			w.WriteAttributeString("nodesTotal", this.NodesTotalLabel.Content.ToString());
			w.WriteAttributeString("fields", this.FieldsLabel.Content.ToString());
			// TODO: Nr errors.

			if (Fields.Count > 0)
				SensorDataServerRequest.OutputFields(w, Fields, this.request.SeqNr, true, null);

			// TODO: Failure, if errors reported.

			w.WriteEndElement();
			w.Flush();
		}

		public void OpenButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				OpenFileDialog Dialog = new OpenFileDialog()
				{
					AddExtension = true,
					CheckFileExists = true,
					CheckPathExists = true,
					DefaultExt = "xml",
					Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*",
					Multiselect = false,
					ShowReadOnly = true,
					Title = "Open sensor data readout"
				};

				bool? Result = Dialog.ShowDialog(MainWindow.FindWindow(this));

				if (Result.HasValue && Result.Value)
				{
					XmlDocument Xml = new XmlDocument();
					Xml.Load(Dialog.FileName);

					this.Load(Xml, Dialog.FileName);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Unable to load file.", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		public void Load(XmlDocument Xml, string FileName)
		{
			List<Field> Fields;
			XmlElement E;
			
			XSL.Validate(FileName, Xml, sensorDataRoot, sensorDataNamespace, schema1, schema2);

			this.SensorDataListView.Items.Clear();
			this.nodes.Clear();
			this.failed.Clear();

			this.StateLabel.Content = XML.Attribute(Xml.DocumentElement, "state", string.Empty);
			this.NodesOkLabel.Content = XML.Attribute(Xml.DocumentElement, "nodesOk", string.Empty);
			this.NodesFailedLabel.Content = XML.Attribute(Xml.DocumentElement, "nodesFailed", string.Empty);
			this.NodesTotalLabel.Content = XML.Attribute(Xml.DocumentElement, "nodesTotal", string.Empty);
			this.FieldsLabel.Content = XML.Attribute(Xml.DocumentElement, "fields", string.Empty);

			foreach (XmlNode N in Xml.DocumentElement.ChildNodes)
			{
				E = N as XmlElement;
				if (E == null)
					continue;

				switch (E.LocalName)
				{
					case "fields":
						Fields = SensorClient.ParseFields(E, out bool Done);
						foreach (Field Field in Fields)
						{
							this.nodes[Field.Thing.Key] = true;
							this.SensorDataListView.Items.Add(new FieldItem(Field));
						}
						break;

					// TODO: failure
				}
			}
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (this.SensorDataListView.View is GridView GridView)
			{
				object Value;

				Value = Registry.GetValue(MainWindow.registryKey, "SensorDataTimestampWidth", (int)GridView.Columns[0].Width);
				if (Value != null && Value is int)
					GridView.Columns[0].Width = (int)Value;

				Value = Registry.GetValue(MainWindow.registryKey, "SensorDataFieldWidth", (int)GridView.Columns[1].Width);
				if (Value != null && Value is int)
					GridView.Columns[1].Width = (int)Value;

				Value = Registry.GetValue(MainWindow.registryKey, "SensorDataValueWidth", (int)GridView.Columns[2].Width);
				if (Value != null && Value is int)
					GridView.Columns[2].Width = (int)Value;

				Value = Registry.GetValue(MainWindow.registryKey, "SensorDataUnitWidth", (int)GridView.Columns[3].Width);
				if (Value != null && Value is int)
					GridView.Columns[3].Width = (int)Value;

				Value = Registry.GetValue(MainWindow.registryKey, "SensorDataStatusWidth", (int)GridView.Columns[4].Width);
				if (Value != null && Value is int)
					GridView.Columns[4].Width = (int)Value;

				Value = Registry.GetValue(MainWindow.registryKey, "SensorDataTypeWidth", (int)GridView.Columns[5].Width);
				if (Value != null && Value is int)
					GridView.Columns[5].Width = (int)Value;
			}

		}

	}
}
