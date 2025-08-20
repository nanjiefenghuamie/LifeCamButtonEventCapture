using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using HidSharp;
/// <summary>
///lifecam可用，但是会超时报错。
/// </summary>
class Program
{
    // 配置：请把这里的 VID/PID 替换成你的 LifeCam USB 麦克风的实际值
    const int Vid = 0x045E; // Microsoft 的 VID 常见值之一，示例，请替换
    const int Pid = 0x0811; // 示例，请替换成你的设备 PID

    //VID_17EF&PID_608C
    // 防抖/重复触发的简单实现
    static long lastPressedTicks = 0;
    static readonly int DebounceMs = 200;

    static void Main(string[] args)
    {
        Console.WriteLine("LifeCam 麦克风按钮监听程序 ( HidSharp )");
        var list = DeviceList.Local;

        // 方式 A：按 VID/PID 筛选
        var devices = list.GetHidDevices(Vid, Pid).ToArray();

        // 如果没有发现，尝试备用筛选：设备名包含 "LifeCam" 或 "Microsoft" 的 HID 设备
        if (devices.Length == 0)
        {
            Console.WriteLine("未按 VID/PID 匹配到设备，尝试按名称筛选...");
            devices = list.GetHidDevices().Where(d =>
                (d.GetProductName() ?? "").IndexOf("LifeCam", StringComparison.OrdinalIgnoreCase) >= 0 ||
                (d.GetProductName() ?? "").IndexOf("Microsoft", StringComparison.OrdinalIgnoreCase) >= 0
            ).ToArray();
        }

        if (devices.Length == 0)
        {
            Console.WriteLine("未找到可用的 HID 设备，请确认设备已连接且驱动正常。");
            return;
        }

        foreach (var device in devices)
        {
            Console.WriteLine($"Found device: {device.GetProductName()} (VID={device.VendorID:X4}, PID={device.ProductID:X4})");
            try
            {
                // 2) 打开设备并读取输入报告
                // 使用 ReadOnly stream 来读取中断输入报告
                using (var stream = device.Open())
                {
                    // 输入报告长度通常不大，64 字节是常见的上限，按设备实际调整
                    var buffer = new byte[64];
                    Console.WriteLine("开始读取输入报告，按 Ctrl+C 退出。");

                    while (true)
                    {
                        int bytesRead = stream.Read(buffer, 0, buffer.Length);
                        if (bytesRead > 0)
                        {
                            ParseInputReport(buffer, bytesRead);
                        }
                        // 简单避免 CPU 100% 使用；如果你愿意，可以改为异步/事件驱动
                        Thread.Sleep(2);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取设备时发生异常：{ex.Message}");
            }

            Console.WriteLine("离开设备循环，尝试下一个设备（如果有）。");
        }

        Console.WriteLine("未找到可用设备或已退出。");
    }

    // 根据你的设备输入报告描述符解析按钮事件
    // 你需要把这里替换成基于实际报告的解析逻辑
    // 典型场景：某些按钮映射在 report[1] 的某个位，或在某个字节的特定位
    static void ParseInputReport(byte[] report, int length)
    {
        // 例子占位：请按实际设备报告格式修改
        // 这里假设 report[0] 是报告 ID，report[1] 是按钮状态字节
        if (length < 2)
            return;

        byte btnState = report[1];

        // 示例：假设有三个按钮，分别占 bit0, bit1, bit2
        bool button1 = (btnState & 0x01) != 0;
        bool button2 = (btnState & 0x02) != 0;
        bool button3 = (btnState & 0x04) != 0;

        // 去抖：只对新 presses 生效
        long now = Stopwatch.GetTimestamp();
        if (button1 || button2 || button3)
        {
            // 简单去抖：只在最近 200ms 内允许重复触发
            if ((now - lastPressedTicks) / (double)Stopwatch.Frequency * 1000.0 < DebounceMs)
            {
                return;
            }
            lastPressedTicks = now;
        }

        if (button1)
        {
            Console.WriteLine("Button 1 pressed (来自设备 report[1] bit0)");
            StartStopRecording();
        }
        if (button2)
        {
            Console.WriteLine("Button 2 pressed (来自设备 report[1] bit1)");
            StartStopRecording();
        }
        if (button3)
        {
            Console.WriteLine("Button 3 pressed (来自设备 report[1] bit2)");
            StartStopRecording();
        }

        // 你可以在这里把检测到的按钮事件转发到你的应用逻辑，例如：
        // - 调用音频库开始/停止录音
        // - 调整降噪、静音等设置
    }

    // 你的实际录音控制逻辑示例
    static void StartStopRecording()
    {
        // TODO：替换成你自己的录音实现
        Console.WriteLine("触发录音开关逻辑（请在此实现你的实际录音代码）");
    }
}