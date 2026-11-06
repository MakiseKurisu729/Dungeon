using System.Collections.Generic;

/// <summary>
/// 断言唯一的证据出口（R-8 / AC-10）：断言方法只认这个接口，不认识落盘、计分、引导。
/// 实现方：<see cref="QaEvidence"/>。M7 后可替换为内存收集器或 NUnit 结果适配器，断言源码不变。
/// </summary>
public interface IQaSink
{
    /// <param name="kind">production = 产品断言；infra = 测试基础设施</param>
    /// <param name="status">pass / product_fail / blocked / observed / infra_fail</param>
    void Record(string qaItem, string assertion, object expected, object actual,
                bool pass, string status, string kind, Dictionary<string, object> data);

    void BlockedAssertion(string qaItem, string assertion, object expected, string reason,
                          Dictionary<string, object> data);

    void InfraFailure(string qaItem, string assertion, string message, string stack);

    /// <summary>本轮证据落点（断言只用于日志/数据字段，不做写操作）。</summary>
    string EvidenceDir { get; }
}
