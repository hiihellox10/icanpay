using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace ICanPay.Test
{
    /// <summary>
    /// ����֪ͨ���Թ���
    /// </summary>
    public static class TestNotifyUtility
    {
        /// <summary>
        /// ��ȡ����֪ͨ����
        /// </summary>
        public static TestNotifyData GetNotify()
        {
            TestNotifyData notify = new TestNotifyData();
            notify.NotifyData = ReadNotifyData();
            notify.IP = HttpContext.Current.Request.UserHostAddress;
            notify.Url = HttpContext.Current.Request.RawUrl;
            notify.DateTime = DateTime.Now;

            return notify;
        }


        /// <summary>
        /// ��ȡ���ط��ص�����
        /// </summary>
        private static Dictionary<string, string> ReadNotifyData()
        {
            Dictionary<string, string> notifyData = new Dictionary<string, string>();
            System.Collections.Specialized.NameValueCollection coll;
            string[] keys;

            // ��ȡͨ��Get�����ֵ
            coll = HttpContext.Current.Request.QueryString;
            keys = coll.AllKeys;
            for (int i = 0; i < keys.Length; i++)
            {
                notifyData[keys[i]] = coll[keys[i]];
            }

            // ��ȡͨ��Post�����ֵ
            coll = HttpContext.Current.Request.Form;
            keys = coll.AllKeys;
            for (int i = 0; i < keys.Length; i++)
            {
                notifyData[keys[i]] = coll[keys[i]];
            }

            return notifyData;
        }
    }
}