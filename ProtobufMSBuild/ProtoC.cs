using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace ProtobufMSBuild
{
	internal static class SharedProtobufMSBuild {
		internal struct ProtobufLanguageInfo
		{
			// %S = Sentence Case File Name
			// %s = File Name (no modification)
			// %f = Folder
			public string FileName { get; }

			public ProtobufLanguageInfo(string fileName)
			{
				FileName = fileName;
			}
		}

		internal static ImmutableDictionary<string, IList<ProtobufLanguageInfo>> ProtobufLanguageToFileType = new Dictionary<string, IList<ProtobufLanguageInfo>>(StringComparer.OrdinalIgnoreCase)
		{
			{ "csharp", new List<ProtobufLanguageInfo> { new ProtobufLanguageInfo("%S.cs") } },

			{ "cpp", new List<ProtobufLanguageInfo> { new ProtobufLanguageInfo("%f%s.pb.cc"), new ProtobufLanguageInfo("%f%s.pb.h") } },
			{ "java", new List<ProtobufLanguageInfo> { new ProtobufLanguageInfo("%f%s/%S.java") } },
			//{ "kotlin", new List<ProtobufLanguageInfo> { new ProtobufLanguageInfo("%s.java"), new ProtobufLanguageInfo("%s.kt") } }, // Note: Protobuf's Kotlin plugin generates both .kt and .java files
			{ "python", new List<ProtobufLanguageInfo> { new ProtobufLanguageInfo("%f%s_pb2.py") } },
			{ "objc", new List<ProtobufLanguageInfo> { new ProtobufLanguageInfo("%f%S.pbobjc.m"), new ProtobufLanguageInfo("%f%S.pbobjc.h") } },
			//{ "php", new List<ProtobufLanguageInfo> { new ProtobufLanguageInfo("%s.php") } }, // Note: Protobuf's PHP generator uses message names for file names.
			{ "ruby", new List<ProtobufLanguageInfo> { new ProtobufLanguageInfo("%f%s_pb.rb") } }
			// rust seems too complicated rn.
		}.ToImmutableDictionary();

		// standard 2.0 doesn't have path.GetRelativePath. Naive implementation.
		internal static string GetRelativePath(string file, string projectRoot)
		{
			if (string.IsNullOrEmpty(projectRoot))
			{
				throw new ArgumentException("Project root cannot be empty", nameof(projectRoot));
			}

			// C:\Project\stuff\file.proto -> stuff\file.proto

			// This should squash most of the bugs a user should experience...
			if (!file.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
			{
				throw new ArgumentException($"File '{file}' is not under project root '{projectRoot}'", nameof(file));
			}

			return file.Replace(projectRoot, string.Empty).TrimStart(Path.DirectorySeparatorChar).TrimStart(Path.AltDirectorySeparatorChar);
		}

		internal static string TransformProtobufLanguageInfoFileName(string file, string projectRoot, string compiledProtobufsDirectory, string fileNamePattern)
		{
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
			string sentenceCaseFileName = char.ToUpper(fileNameWithoutExtension[0], System.Globalization.CultureInfo.InvariantCulture) + fileNameWithoutExtension.Substring(1);
			//string folder = GetRelativePath(file, projectRoot);
			string folder = Path.GetDirectoryName(file); // already relative lol
			string transformedFileName = fileNamePattern.Replace("%S", sentenceCaseFileName)
													    .Replace("%s", fileNameWithoutExtension)
												   	    .Replace("%f", !string.IsNullOrEmpty(folder) ? folder + Path.DirectorySeparatorChar : string.Empty);
			return Path.Combine(Path.Combine(projectRoot, compiledProtobufsDirectory), transformedFileName);
		}

		internal static IList<string> GetProtobufFile(string file, string projectRoot, string compiledProtobufsDirectory, string protobufLanguage)
		{
			IList<ProtobufLanguageInfo> extensions;
			try
			{
				 extensions = ProtobufLanguageToFileType[protobufLanguage];
			} catch (KeyNotFoundException)
			{
				throw new ArgumentException($"Unsupported Protobuf language: {protobufLanguage}");
			}
			var result = new List<string>();
			foreach (ProtobufLanguageInfo langInfo in extensions)
			{
				result.Add(System.IO.Path.Combine(System.IO.Path.Combine(projectRoot, compiledProtobufsDirectory), TransformProtobufLanguageInfoFileName(file, projectRoot, compiledProtobufsDirectory, langInfo.FileName)));
			}
			return result;
		}

		internal static string Transform(string protobufs, string projectRoot, string compiledProtobufsDirectory, string protobufLanguage)
		{
			if (string.IsNullOrEmpty(protobufs)) return string.Empty;
			var files = protobufs.Split(';');
			for (int i = 0; i < files.Length; i++)
			{
				files[i] = string.Join(";", from currentLanguage in protobufLanguage.Split(';')
						                     select string.Join(";", GetProtobufFile(files[i], projectRoot, compiledProtobufsDirectory, currentLanguage)));
			}
			return string.Join(";", files);
		}
	}

	public class ProtobufMSBuildDetermine : Task
	{
		[Required]
		public string Protobufs { get; set; }

		[Required]
		public string ProtobufLanguage { get; set; }

		[Required]
		public string ProjectRoot { get; set; }

		[Required]
		public string ProtobufCompiledDirectory { get; set; }

		[Output]
		public string CompiledProtobufs { get; set; }

		[Output]
		public ITaskItem[] CSCompiledProtobufsAsTaskItem { get; set; }

		public override bool Execute()
		{
			Log.LogMessage(MessageImportance.High, $"ProjectRoot: {ProjectRoot}");

			//CompiledProtobufs = Protobufs + ".cs";
			//Log.LogWarning(CompiledProtobufs);
			CompiledProtobufs = SharedProtobufMSBuild.Transform(Protobufs, ProjectRoot, ProtobufCompiledDirectory, ProtobufLanguage);
			CSCompiledProtobufsAsTaskItem = (from compiledProtobuf in CompiledProtobufs.Split(';')
											 where compiledProtobuf.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
											 select new TaskItem(compiledProtobuf)).ToArray();

			foreach (var file in CompiledProtobufs.Split(';'))
			{
					Log.LogMessage(MessageImportance.High, $"generated: {file}");
			}

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
		public string ProtobufLanguage { get; set; }

		[Required]
		public string ProjectRoot { get; set; }

		[Output]
		public string CompiledProtobufs { get; set; }

		public override bool Execute()
		{
			Log.LogMessage(MessageImportance.High, ProtobufCompiledDirectory);

			CompiledProtobufs = SharedProtobufMSBuild.Transform(Protobufs, ProjectRoot, ProtobufCompiledDirectory, ProtobufLanguage);

			foreach (var file in Protobufs.Split(';'))
			{
				if (!System.IO.File.Exists(file))
				{
					Log.LogError($"Protobuf file '{file}' does not exist.");
				}

				foreach (var protobufLanguage in ProtobufLanguage.Split(';'))
				{
					Log.LogMessage(MessageImportance.High, $"Compiling Protobuf file for language {protobufLanguage}: {file}");

					using (Process protocProcess = new Process())
					{
						protocProcess.StartInfo.UseShellExecute = false;
						protocProcess.StartInfo.FileName = ProtoCompiler;
						protocProcess.StartInfo.CreateNoWindow = true;
						protocProcess.StartInfo.Arguments = $"-I{ProjectRoot} --{protobufLanguage}_out {ProtobufCompiledDirectory} {System.IO.Path.Combine(ProjectRoot, file)}";
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

						Log.LogCommandLine(MessageImportance.Low, $"{protocProcess.StartInfo.FileName} {protocProcess.StartInfo.Arguments}");
						protocProcess.Start();

						protocProcess.WaitForExit();

						if (protocProcess.ExitCode != 0)
						{
							Log.LogError($"Protobuf compilation failed for file '{file}' and language {protobufLanguage}. Exit code: {protocProcess.ExitCode}");
						}
						else
						{
							Log.LogMessage($"Successfully compiled Protobuf file for language {protobufLanguage}: {file}");
						}
					}
				}
			}

			return !Log.HasLoggedErrors;
			//throw new NotImplementedException();
		}
	}
}
