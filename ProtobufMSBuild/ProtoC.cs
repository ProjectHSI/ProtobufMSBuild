using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace ProtobufMSBuild
{
	public class ProtoC : Task
	{
		[Required]
		string Protobufs { get; set; }

		[Output]
		string CompiledProtobufs { get; set; }

		public override bool Execute()
		{
			//CompiledProtobufs = "test";
			Log.LogWarning(CompiledProtobufs);

			return Log.HasLoggedErrors;
			//throw new NotImplementedException();
		}
	}
}
