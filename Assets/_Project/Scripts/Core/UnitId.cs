using System;
namespace Dungeon.Core {

    /// <summary>单位身份句柄：由 <see cref="UnitRegistry"/> 在注册时分配。
    /// 同一进程（一次 Play 会话）内唯一、只增不减；无效值（未注册/已注销）= <see cref="None"/>。
    /// 只是运行时句柄，<b>不是</b>跨会话稳定 ID（稳定身份留给后续网络里程碑）。</summary>
    public readonly struct UnitId : IEquatable<UnitId>
    {
        /// <summary>无效句柄（未注册 / 已注销 / 跨进程）。</summary>
        public static readonly UnitId None = default;

        /// <summary>运行时句柄；同一进程内唯一、递增，0 表示无效。</summary>
        public int Value { get; }

        /// <summary>是否有效（<see cref="Value"/> != 0）。</summary>
        public bool IsValid { get { return Value != 0; } }

        public UnitId(int value) { Value = value; }

        public bool Equals(UnitId other) { return Value == other.Value; }

        public override bool Equals(object obj) { return obj is UnitId other && Equals(other); }

        public override int GetHashCode() { return Value; }

        public static bool operator ==(UnitId a, UnitId b) { return a.Value == b.Value; }
        public static bool operator !=(UnitId a, UnitId b) { return a.Value != b.Value; }

        public override string ToString()
        {
            return IsValid ? "UnitId(" + Value + ")" : "UnitId(None)";
        }
    }
}
