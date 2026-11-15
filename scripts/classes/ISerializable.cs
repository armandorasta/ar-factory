namespace ArFactory.Serialization;

internal interface ISerializable<TData> where TData : struct
{
	TData CaptureSerializationData();
	void RestoreFromSerializationData(ref TData data);
}
