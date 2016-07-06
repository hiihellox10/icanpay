# 实现基类支持更多支付网关 #

  * [主要基类、接口介绍](http://code.google.com/p/icanpay/wiki/ImplementAnGateway#主要基类、接口介绍)
  * [让ICanPay支持新的支付网关](http://code.google.com/p/icanpay/wiki/ImplementAnGateway#让ICanPay支持新的支付网关)
    * [PayGateway基类介绍](http://code.google.com/p/icanpay/wiki/ImplementAnGateway#PayGateway基类介绍)
    * [支付网关通知的验证](http://code.google.com/p/icanpay/wiki/ImplementAnGateway#支付网关通知的验证)
    * [实现创建订单接口](http://code.google.com/p/icanpay/wiki/ImplementAnGateway#实现创建订单接口)
    * [实现创建查询接口](http://code.google.com/p/icanpay/wiki/ImplementAnGateway#实现创建查询接口)


---

# 主要接口介绍 #

**PayGateway**

所有的支付网关的基类，新增加支持的支付网关必须继承自PayGateway。

**IPaymentForm**

当支付网关是通过创建一个包含订单数据的form表单来提交订单时通过实现IPaymentForm接口来创建支付订单的html页面代码。

**IPaymentUrl**

当支付网关是通过创建一个包含订单数据的url来提交订单时通过实现IPaymentUrl接口来创建订单的url。

**IQueryForm**

当支付网关是通过创建一个包含订单数据的form表单来查询订单时通过实现IQueryForm接口来创建查询订单的html页面代码。查询结果跟订单支付成功时的通知返回方式一致。

**IQueryUrl**

当支付网关是通过创建一个包含订单数据的url来查询订单时通过实现IPaymentUrl接口来创建订单的url。查询结果跟订单支付成功时的通知返回方式一致。

**IQueryPayment**

通过向支付网关查询url发送需要查询的订单数据，支付网关在查询url页面输出查询结果时实现此接口。


---

# 让ICanPay支持新的支付网关 #

下面演示如何增加新的支付网关，我们新建一个名为DemoGateway的新支付网关。

需要完成的任务分别有实现创建支付订单功能、创建查询订单功能、处理支付网关返回的支付结果。


---

# PayGateway基类介绍 #
首先我们需要让DemoGateway能够处理支付网关返回的支付结果部分。新建的DemoGateway它需要继承自PayGateway基类。

```
public sealed class DemoGateway : PayGateway
{
}
```
接着我们需要修改ICanPay.GatewayType枚举，在ICanPay.GatewayType枚举中增加Demo值来表示当前网关为DemoGateway。
```
public enum GatewayType
{
    // 省略已存在枚举值

    Demo = 6
}
```
再修改DemoGateway.GatewayType属性，让它返回新增的GatewayType.Demo值。
```
public sealed class DemoGateway : PayGateway
{
    public override GatewayType GatewayType
    {
        get { return GatewayType.Demo }
    }
}
```
我们还需要实现PaymentNotifyMethod属性。订单的支付结果目前会通过2种形式返回，1
、支付网关服务器发送支付结果通知到指定url，2、将用户跳转到设置的通知url，并在查询字符串里包含支付结果。

有的支付网关可能通过服务器发送支付结果通知给你时会要求你输出一个特定的字符串用户表示已接收到支付通知，而将用户跳转到通知url时则不需要。如果没有对这2种方式做判断，在用户跳转到通知url时输出表示接收到支付网站通知的字符串时会让用户觉得很奇怪，这时应该给用户一个支付成功的提示页面，而不是奇怪的字符串。

支付通知是否由网关服务器发送，我们可以通过HttpContext.Current.Request的RequestType、UserAgent 2个属性来判断，因为支付网关的服务器通知的这2个属性都是固定值，而用户通过浏览器返回支付通知页面时的这2个属性会明显不同。这2个属性你可以先运行几次你需要新增的支付网关提供的Demo，完成支付并获得相关值。
```
public sealed class DemoGateway : PayGateway
{
    public override PaymentNotifyMethod PaymentNotifyMethod
    {
        get
        {
            if (string.Compare(HttpContext.Current.Request.RequestType, "POST") == 0 &&
                string.Compare(HttpContext.Current.Request.UserAgent, "Mozilla/4.0") == 0)
            {
                return PaymentNotifyMethod.ServerNotify;
            }

            return PaymentNotifyMethod.AutoReturn;
        }
    }
}
```

WriteSucceedFlag方法在收到支付网关通知时输出表示接收到支付通知的字符串，请参考相关支付网关的文档，支付网关没有要求时可省略。


---

# 支付网关通知的验证 #
CheckNotifyData方法用于验证当前支付网关返回的支付结果。支付网关返回的支付通知数据(查询字符串、from表单)都保存在PayGateway.GatewayParameterData属性中，你可以使用GetGatewayParameterValue方法来获得相应参数的值。支付结果的验证请参考相关支付网关的文档。

我们还需要让ICanPay能够识别支付通知是否是DemoGateway发送的。先在NotifyProcess类中增加IsDemoGateway属性，在IsDemoGateway属性中我们通过判断当前支付通知中是否存在DemoGateway的支付通知必须包含的参数来判断当前支付通知是否是DemoGateway支付网关发送的，相关参数请阅读相关支付网关的文档。

```
internal static class NotifyProcess
{
    static string[] demoGatewayVerifyParmaNames= { "c_mid", "c_order", "c_orderamount", "c_ymd", "c_succmark" };

    private static bool IsDemoGateway
    {
        get
        {
            return Utility.ExistParameter(demoGatewayVerifyParmaNames, gatewayParameterData);
        }
    }
}
```

接着修改NotifyProcess.GetGateway方法，让它在当前支付通知是由DemoGateway发送时返回DemoGateway的实例。

```
internal static class NotifyProcess
{
    public static PayGateway GetGateway()
    {
        // 省略已存在代码

        if (IsDemoGateway)
        {
            return new DemoGateway(gatewayParameterData);
        }

        return new NullGateway(gatewayParameterData);
    }
}
```

完成上面的这些步骤之后我们的DemoGateway已经可以处理支付网关的支付通知了。


---

# 实现创建订单接口 #
阅读你需要支持的支付网关的文档，当它们是通过from表单提交订单时你需要继承IPaymentForm接口，当它们是通过ul表单提交订单时你需要继承IPaymentUrl接口。

PayGateway基类中提供了一些helper方法帮助你更方便的创建支付订单，详情请阅读已实现的支付网关的代码。


---

# 实现创建查询接口 #
阅读你需要支持的支付网关的文档，它们是否提供查询接口。当它们是通过from表单提交订单时你需要继承IQueryForm接口，当它们是通过ul表单提交订单时你需要继承IQueryUrl接口。

如果是需要向支付网关查询url发送需要查询的订单数据，支付网关在查询url页面输出查询结果时你需要继承IQueryPayment接口。