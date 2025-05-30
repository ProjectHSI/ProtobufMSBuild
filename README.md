# ProtobufMSBuild

## How to Use

1. Install "Protoc". See the Protobuf Documentation (at this time `Protobuf.Compiler.Tools` does **not** work).
2. Add to project
3. Create a .proto file. If you really want to, set the file type to `Protobuf`, but this is not required.
4. Build the project - it doesn't matter whether it has compile-time issues or not, it just needs to be built to run the "ProtobufCompile" target.
5. Import the resulting C# namespace (defined in the Protobuf file).