#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;

namespace CustomRegister
{
	[Tool]
	public class Plugin : EditorPlugin
	{
		private List<string> _scripts;
		private Control _control;
		private LineEdit _sourcePathLineEdit;
		private LineEdit _classPrefixLineEdit;
		private string _sourcePath;
		private string _classPrefix;
		
		public override void _EnterTree()
		{
			_scripts = new List<string>();
			CreateCustomRegisterControl();
			AddControlToBottomPanel(_control, "Custom Register");
			RegisterCustomClasses();
		}

		public override void _ExitTree()
		{
			UnregisterCustomClasses();
			RemoveControlFromBottomPanel(_control);
			_control = null;
		}

		private void RegisterCustomClasses()
		{
			_scripts.Clear();
			var file = new File();
			
			foreach (var type in GetCustomResources())
			{
				string path = ClassPath(type);
				if (!file.FileExists(path)) continue;
				var script = GD.Load<Script>(path);
				if (script == null) continue;
				AddCustomType($"{_classPrefix}{type.Name}", nameof(Resource), script, null);
				GD.Print($"Register custom resource: {type.Name} -> {path}");
				_scripts.Add(type.Name);
			}

			foreach (var type in GetCustomNodes())
			{
				string path = ClassPath(type);
				if (!file.FileExists(path)) continue;
				var script = GD.Load<Script>(path);
				if (script == null) continue;
				AddCustomType($"{_classPrefix}{type.Name}", nameof(Node), script, null);
				GD.Print($"Register custom node: {type.Name} -> {path}");
				_scripts.Add(type.Name);
			}
		}

		private string ClassPath(Type type)
		{
			return $"{_sourcePath}/{type.Namespace?.Replace(".", "/") ?? ""}/{type.Name}.cs";
		}

		private IEnumerable<Type> GetCustomResources()
		{
			var assembly = Assembly.GetAssembly(typeof(Plugin));
			return assembly.GetTypes().Where(
				t => !t.IsAbstract && t.IsSubclassOf(typeof(Resource)) &&
					t.GetCustomAttributes(typeof(RegisterAttribute), false).Length > 0);
		}

		private IEnumerable<Type> GetCustomNodes()
		{
			var assembly = Assembly.GetAssembly(typeof(Plugin));
			return assembly.GetTypes().Where(
				t => !t.IsAbstract && t.IsSubclassOf(typeof(Node)) &&
					t.GetCustomAttributes(typeof(RegisterAttribute), false).Length > 0);
		}

		private void UnregisterCustomClasses()
		{
			foreach (string script in _scripts)
			{
				RemoveCustomType(script);
				GD.Print($"Unregister custom resource: {script}");
			}
			_scripts.Clear();
		}

		private void CreateCustomRegisterControl()
		{
			PackedScene scene = GD.Load<PackedScene>("res://addons/CustomRegister/CustomRegisterControl.tscn");
			if (scene.Instance() is Control control) _control = control;
			_sourcePathLineEdit =
				_control.GetNode<LineEdit>("MarginContainer/VBoxContainer/SourcePath/LineEdit");
			_classPrefixLineEdit =
				_control.GetNode<LineEdit>("MarginContainer/VBoxContainer/ClassPrefix/LineEdit");
			Button button = _control.GetNode<Button>("MarginContainer/VBoxContainer/Button");
			button.Connect("pressed", this, nameof(OnRegisterButtonPressed));
		}

		private void OnRegisterButtonPressed()
		{
			UnregisterCustomClasses();
			_sourcePath = _sourcePathLineEdit.Text;
			_classPrefix = _classPrefixLineEdit.Text;
			RegisterCustomClasses();
		}
	}
}
#endif