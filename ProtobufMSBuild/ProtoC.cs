using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Diagnostics;

namespace ProtobufMSBuild
{
	internal static class SharedProtobufMSBuild {
		internal static string GetProtobufFile(string file, string projectRoot, string compiledProtobufsDirectory)
		{
			return System.IO.Path.Combine(System.IO.Path.Combine(projectRoot, compiledProtobufsDirectory), System.IO.Path.GetFileNameWithoutExtension(file) + ".cs");
		}

		internal static string Transform(string protobufs, string projectRoot, string compiledProtobufsDirectory)
		{
			if (string.IsNullOrEmpty(protobufs)) return string.Empty;
			var files = protobufs.Split(';');
			for (int i = 0; i < files.Length; i++)
			{
				files[i] = GetProtobufFile(files[i], projectRoot, compiledProtobufsDirectory);
			}
			return string.Join(";", files);
		}
	}

	public class ProtobufMSBuildDetermine : Task
	{
		[Required]
		public string Protobufs { get; set; }

		[Required]
		public string ProjectRoot { get; set; }

		[Required]
		public string ProtobufCompiledDirectory { get; set; }

		[Output]
		public string CompiledProtobufs { get; set; }

		public override bool Execute()
		{
			//CompiledProtobufs = Protobufs + ".cs";
			//Log.LogWarning(CompiledProtobufs);
			CompiledProtobufs = SharedProtobufMSBuild.Transform(Protobufs, ProjectRoot, ProtobufCompiledDirectory);

			return !Log.HasLoggedErrors;
			//throw new NotImplementedException();
		}
	}

	public class ProtobufMSBuildCompile : Task
	{
		[Required]
		public string Protobufs { get; set; }

		[Required]
		public string ProtobufCompiledDirectory { get; set; }

		[Required]
		public string ProtoCompiler { get; set; }

		[Required]
		public string ProtoLanguage { get; set; }

		[Required]
		public string ProjectRoot { get; set; }

		[Output]
		public string CompiledProtobufs { get; set; }

		public override bool Execute()
		{
			CompiledProtobufs = SharedProtobufMSBuild.Transform(Protobufs, ProjectRoot, ProtobufCompiledDirectory);

			foreach (var file in Protobufs.Split(';'))
			{
				if (!System.IO.File.Exists(file))
				{
					Log.LogError($"Protobuf file '{file}' does not exist.");
				}

				Log.LogMessage($"Compiling Protobuf file: {file}");

				using (Process protocProcess = new Process())
				{
					protocProcess.StartInfo.UseShellExecute = false;
					protocProcess.StartInfo.FileName = ProtoCompiler;
					protocProcess.StartInfo.CreateNoWindow = true;
					protocProcess.StartInfo.Arguments = $"-I{ProjectRoot} --${ProtoLanguage}_out {ProtobufCompiledDirectory} {System.IO.Path.Combine(ProjectRoot, file)}";
					protocProcess.StartInfo.WorkingDirectory = ProjectRoot;
					protocProcess.StartInfo.RedirectStandardOutput = true;
					protocProcess.StartInfo.RedirectStandardError = true;

					protocProcess.OutputDataReceived += (sender, e) =>
					{
						if (!string.IsNullOrEmpty(e.Data))
						{
							Log.LogMessage(MessageImportance.Normal, e.Data);
						}
					};

					protocProcess.ErrorDataReceived += (sender, e) =>
					{
						if (!string.IsNullOrEmpty(e.Data))
						{
							Log.LogWarning(e.Data);
						}
					};

					Log.LogMessage(MessageImportance.Low, $"{protocProcess.StartInfo.FileName} {protocProcess.StartInfo.Arguments}");
					protocProcess.Start();

					protocProcess.WaitForExit();

					if (protocProcess.ExitCode != 0)
					{
						Log.LogError($"Protobuf compilation failed for file '{file}'. Exit code: {protocProcess.ExitCode}");
					}
					else
					{
						Log.LogMessage($"Successfully compiled Protobuf file: {file}");
					}
				}
			}

			return !Log.HasLoggedErrors;
			//throw new NotImplementedException();
		}
	}
}
