using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace ICanPay.Providers
{
    /// <summary>
    /// 支付宝网关
    /// </summary>
    /// <remarks>
    /// 当前支付宝的实现仅支持MD5密钥。
    /// </remarks>
    public sealed class AlipayGateway : GatewayBase, IPaymentForm, IPaymentUrl
    {

        #region 私有字段

        private const string PayGatewayUrl = "https://mapi.alipay.com/gateway.do";

        #endregion


        #region 构造函数

        /// <summary>
        /// 初始化支付宝网关
        /// </summary>
        public AlipayGateway()
        {
        }


        /// <summary>
        /// 初始化支付宝网关
        /// </summary>
        /// <param name="gatewayParameterList">网关通知的数据集合</param>
        public AlipayGateway(Dictionary<string, GatewayParameter> gatewayParameterList)
            : base(gatewayParameterList)
        {
        }

        #endregion


        #region 属性

        public override GatewayType GatewayType
        {
            get
            {
                return GatewayType.Alipay;
            }
        }


        public override PaymentNotifyMethod PaymentNotifyMethod
        {
            get
            {
                // 通过RequestType、UserAgent来判断是否为服务器通知
                if (string.Compare(HttpContext.Current.Request.RequestType, "POST") == 0 &&
                    string.Compare(HttpContext.Current.Request.UserAgent, "Mozilla/4.0") == 0)
                {
                    return PaymentNotifyMethod.ServerNotify;
                }

                return PaymentNotifyMethod.AutoReturn;
            }
        }


        protected override Encoding PageEncoding
        {
            get
            {
                return Encoding.GetEncoding("GB2312");
            }
        }


        private AlipayMerchant AlipayMerchant
        {
            get
            {
                AlipayMerchant alipayMerchant = Merchant as AlipayMerchant;
                if (alipayMerchant == null)
                {
                    throw new ArgumentException("商户数据类型不是支付宝商户，请将 Merchant 属性设置为 AlipayMerchant 类型", "Merchant");
                }

                return alipayMerchant;
            }
        }


        #endregion


        #region 方法

        public string BuildPaymentForm()
        {
            InitOrderParameter();

            return GetFormHtml(PayGatewayUrl);
        }


        /// <summary>
        /// 初始化订单参数
        /// </summary>
        private void InitOrderParameter()
        {
            SetGatewayParameterValue("service", "create_direct_pay_by_user");
            SetGatewayParameterValue("partner", Merchant.UserName);
            SetGatewayParameterValue("notify_url", Merchant.NotifyUrl);
            SetGatewayParameterValue("return_url", Merchant.NotifyUrl);
            SetGatewayParameterValue("seller_email", AlipayMerchant.SellerEmail);
            SetGatewayParameterValue("sign_type", "MD5");
            SetGatewayParameterValue("subject", Order.Subject);
            SetGatewayParameterValue("out_trade_no", Order.Id);
            SetGatewayParameterValue("total_fee", Order.Amount);
            SetGatewayParameterValue("payment_type", "1");
            SetGatewayParameterValue("_input_charset", "gb2312");
            SetGatewayParameterValue("sign", GetOrderSign());    // 签名需要在最后设置，以免缺少参数。
        }


        public string BuildPaymentUrl()
        {
            InitOrderParameter();

            return string.Format("{0}?{1}", PayGatewayUrl, GetPaymentQueryString());
        }


        private string GetPaymentQueryString()
        {
            return BuildQueryString(GetSortedGatewayParameter());
        }


        /// <summary>
        /// 获得用于签名的参数
        /// </summary>
        private IDictionary<string, string> GetSignParameter()
        {
            SortedDictionary<string, string> result = new SortedDictionary<string, string>();
            foreach(KeyValuePair<string, string> item in GetSortedGatewayParameter())
            {
                // 参数名为 sign，sign_type 的参数不用于签名。
                if (string.Compare("sign", item.Key) != 0 && string.Compare("sign_type", item.Key) != 0)
                {
                    result.Add(item.Key, item.Value);
                }
            }

            return result;
        }


        public override bool ValidateNotify()
        {
            if (IsSuccessResult())
            {
                ReadNotifyOrder();
                return true;
            }

            return false;
        }


        /// <summary>
        /// 读取通知中的订单金额、订单编号
        /// </summary>
        private void ReadNotifyOrder()
        {
            Order.Amount = GetGatewayParameterValue<double>("total_fee");
            Order.Id = GetGatewayParameterValue("out_trade_no");
        }


        /// <summary>
        /// 是否是已成功支付的支付通知
        /// </summary>
        /// <returns></returns>
        private bool IsSuccessResult()
        {
            if(ValidateNotifyParameter() && ValidateNotifySign())
            {
                if(ValidateNotifyId())
                {
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// 检查支付通知，是否支付成功，签名是否正确。
        /// </summary>
        /// <returns></returns>
        private bool ValidateNotifyParameter()
        {
            // 支付状态是否为成功。
            // TRADE_FINISHED（普通即时到账的交易成功状态）
            // TRADE_SUCCESS（开通了高级即时到账或机票分销产品后的交易成功状态）
            if (CompareGatewayParameterValue("trade_status", "TRADE_FINISHED") ||
                CompareGatewayParameterValue("trade_status", "TRADE_SUCCESS"))
            {
                return true;
            }

            return false;
        }


        /// <summary>
        /// 验证支付宝通知的签名
        /// </summary>
        private bool ValidateNotifySign()
        {
            // 验证通知的签名
            if (CompareGatewayParameterValue("sign", GetOrderSign()))
            {
                return true;
            }

            return false;
        }


        /// <summary>
        /// 将网关参数的集合排序
        /// </summary>
        /// <param name="coll">原网关参数的集合</param>
        private SortedList<string, string> GatewayParameterDataSort(ICollection<GatewayParameter> coll)
        {
            SortedList<string, string> list = new SortedList<string, string>();
            foreach (GatewayParameter item in coll)
            {
                list.Add(item.Name, item.Value);
            }

            return list;
        }


        /// <summary>
        /// 获得订单的签名。
        /// </summary>
        private string GetOrderSign()
        {
            // 获得 MD5 值时需要使用 GB2312 编码，否则主题中有中文时会提示签名异常，并且 MD5 值必须为小写。
            return Utility.GetMD5(BuildQueryString(GetSignParameter()) + Merchant.Key, PageEncoding).ToLower();
        }


        public override void WriteSucceedFlag()
        {
            if (PaymentNotifyMethod == PaymentNotifyMethod.ServerNotify)
            {
                HttpContext.Current.Response.Write("success");
            }
        }


        /// <summary>
        /// 验证网关的通知Id是否有效
        /// </summary>
        private bool ValidateNotifyId()
        {
            // 浏览器自动返回的通知Id会在验证后1分钟失效，
            // 服务器异步通知的通知Id则会在输出标志成功接收到通知的success字符串后失效。
            if (string.Compare(Utility.ReadPage(GetValidateNotifyUrl()), "true") == 0)
            {
                return true;
            }

            return false;
        }


        /// <summary>
        /// 获得验证支付宝通知的Url
        /// </summary>
        private string GetValidateNotifyUrl()
        {
            return string.Format("{0}?service=notify_verify&partner={1}&notify_id={2}", PayGatewayUrl, Merchant.UserName,
                                 GetGatewayParameterValue("notify_id"));
        }

        #endregion

    }
}