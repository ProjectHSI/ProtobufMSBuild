# ProtobufMSBuild

## How to Use

1. Install `protoc`. See the Protobuf Documentation.
2. Add to project
3. Create a `.proto` file. If you really want to, set the file type to `Protobuf`, but this is not required.
4. Build the project - it doesn't matter whether it has compile-time issues or not, it just needs to be built to run the "ProtobufCompile" target.

If you use C#, import the resulting C# namespace (defined in the Protobuf file).

For any other language, add the generated files to your project in your project file (I.E. with `ClCompile`). The resulting file names are consistent with that of the `protoc` tool.

## Supported Languages

| Language           | Supported | Additional Notes                                                                   |
| ------------------ | --------- | ---------------------------------------------------------------------------------- |
| csharp             | Yes       | Default when auto-included.                                                        |
| cpp                | Yes       |                                                                                    |
| dart               | No        | Does not work with current command line arguments. May be supported in the future. |
| go                 | No        | Does not work with current command line arguments. May be supported in the future. |
| java               | Yes       |                                                                                    |
| kotlin             | No        | Untested                                                                           |
| python             | Yes       |                                                                                    |
| objc (Objective-C) | Yes       |                                                                                    |
| php                | No        | Supporting requires `.proto` parsing.                                              |
| ruby               | Yes       |                                                                                    |
| rust               | No        | Output format not well specified.                                                  |

## Properties

| Option Name                           | Description                                           | Default Value                             |
| ------------------------------------- | ----------------------------------------------------- | ----------------------------------------- |
| `NoProtobufMSBuildAutoInclude`        | Prevents automatic inclusion of .proto files          | N/A (false)                               |
| `NoProtobufMSBuildAutoIncludeCompile` | Prevents automatic inclusion of compiled .proto files | N/A (false)                               |
| `ProtobufCompiledDirectory`           | Directory for compiled .proto files                   | `$(IntermediateOutputPath)ProtobufOutput` |

## Item Metadata

| Metadata Name      | Description                                           | Default Value                             |
| ------------------ | ----------------------------------------------------- | ----------------------------------------- |
| `ProtobufLanguage` | The language to compile the .proto file for.          | N/A (C# if included by auto-include)      |