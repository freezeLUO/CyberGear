namespace CyberGear.Control;

/// <summary>
/// can 建造者
/// </summary>
public class CanBusBuilder
{
	/// <summary>
	/// can 配置
	/// </summary>
	public readonly CanbusOption CanbusOption;
	/// <summary>
	/// 插口类型
	/// </summary>
	public readonly SlotType SlotType;
	/// <summary>
	/// 插槽序号
	/// </summary>
	public readonly int SlotIndex;

	/// <summary>
	/// can 建造者
	/// </summary>
	/// <param name="slotType">插口类型</param>
	/// <param name="slotIndex">插槽序号</param>
	public CanBusBuilder(SlotType slotType, int slotIndex)
	{
		CanbusOption = new CanbusOption();
		SlotType = slotType;
		SlotIndex = slotIndex;
	}

	public void Configure(Action<CanbusOption> method)
	{
		method(CanbusOption);
	}

	public CanBus Build()
		=> new CanBus(this);
}
