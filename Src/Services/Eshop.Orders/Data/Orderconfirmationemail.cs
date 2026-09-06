using System.Globalization;
using System.Text;
using Eshop.Orders.Models;

namespace Eshop.Orders.Services.EmailTemplates
{
    public static class OrderConfirmationEmail
    {
        private const string Teal = "#0D4A45";
        private const string Gold = "#C9953A";
        private const string Parchment = "#F5EDD6";
        private const string TextDark = "#1F2A28";

        public static string Build(Order order)
        {
            var itemsHtml = BuildItemsRows(order.OrderItems);

            return $$"""
            <!DOCTYPE html>
            <html dir="rtl" lang="ar">
            <head>
                <meta charset="UTF-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1.0" />
                <title>تأكيد الطلب</title>
                <style>
                    @media only screen and (max-width: 600px) {
                        .email-container { width: 100% !important; }
                        .stack-cell { display: block !important; width: 100% !important; text-align: right !important; }
                    }
                </style>
            </head>
            <body dir="rtl" style="margin:0; padding:0; background-color:{{Parchment}}; font-family:Tahoma, Arial, sans-serif;">
                <table role="presentation" dir="rtl" width="100%" cellpadding="0" cellspacing="0" border="0" style="background-color:{{Parchment}};">
                    <tr>
                        <td align="center" style="padding:24px 12px;">
                            <table role="presentation" dir="rtl" class="email-container" width="100%" cellpadding="0" cellspacing="0" border="0" style="max-width:600px; margin:0 auto; background-color:#FFFFFF; border-radius:16px; overflow:hidden;">

                                <!-- Header -->
                                <tr>
                                    <td align="center" style="background-color:{{Teal}}; padding:32px 24px;">
                                        <div style="font-family:'Amiri','Traditional Arabic',Tahoma,serif; font-size:26px; color:{{Parchment}}; font-weight:bold;">
                                            المكتبة الإسلامية
                                        </div>
                                        <div style="width:60px; height:2px; background-color:{{Gold}}; margin:12px auto 0;"></div>
                                    </td>
                                </tr>

                                <!-- Greeting -->
                                <tr>
                                    <td align="right" style="padding:32px 32px 8px;">
                                        <div align="right" style="font-family:'Amiri','Traditional Arabic',Tahoma,serif; font-size:22px; color:{{Teal}}; font-weight:bold; text-align:right;">
                                            مرحباً {{order.CustomerName}}،
                                        </div>
                                        <p align="right" style="font-size:15px; line-height:1.8; color:{{TextDark}}; margin:12px 0 0; text-align:right;">
                                            تم استلام طلبك بنجاح. سنتواصل معك على رقم <span style="font-weight:bold;">{{order.Phone}}</span> لتأكيد التوصيل، والدفع عند الاستلام.
                                        </p>
                                    </td>
                                </tr>

                                <!-- Order number / date -->
                                <tr>
                                    <td style="padding:16px 32px;">
                                        <table role="presentation" dir="rtl" width="100%" cellpadding="0" cellspacing="0" border="0" style="background-color:{{Parchment}}; border-radius:12px;">
                                            <tr>
                                                <td style="padding:16px 20px;">
                                                    <table role="presentation" dir="rtl" width="100%" cellpadding="0" cellspacing="0" border="0">
                                                        <tr>
                                                            <td width="50%" align="right" style="font-size:13px; color:#6B6355; text-align:right;">رقم الطلب</td>
                                                            <td width="50%" align="right" style="font-size:13px; color:#6B6355; text-align:right;">تاريخ الطلب</td>
                                                        </tr>
                                                        <tr>
                                                            <td width="50%" align="right" style="font-size:16px; font-weight:bold; color:{{Teal}}; padding-top:4px; text-align:right;">{{order.OrderNumber}}</td>
                                                            <td width="50%" align="right" style="font-size:16px; font-weight:bold; color:{{Teal}}; padding-top:4px; text-align:right;">{{order.OrderedAt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)}}</td>
                                                        </tr>
                                                    </table>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>

                                <!-- Items -->
                                <tr>
                                    <td style="padding:8px 32px;">
                                        <table role="presentation" dir="rtl" width="100%" cellpadding="0" cellspacing="0" border="0">
                                            <tr>
                                                <td align="right" style="padding:8px 0; border-bottom:2px solid {{Parchment}}; font-size:13px; color:#6B6355; text-align:right;">المنتج</td>
                                                <td align="center" style="padding:8px 0; border-bottom:2px solid {{Parchment}}; font-size:13px; color:#6B6355; text-align:center;">الكمية</td>
                                                <td align="right" style="padding:8px 0; border-bottom:2px solid {{Parchment}}; font-size:13px; color:#6B6355; text-align:right;">السعر</td>
                                            </tr>
                                            {{itemsHtml}}
                                        </table>
                                    </td>
                                </tr>

                                <!-- Total -->
                                <tr>
                                    <td style="padding:16px 32px 32px;">
                                        <table role="presentation" dir="rtl" width="100%" cellpadding="0" cellspacing="0" border="0" style="background-color:{{Teal}}; border-radius:12px;">
                                            <tr>
                                                <td width="50%" align="right" style="padding:16px 20px; font-size:15px; color:{{Parchment}}; text-align:right;">الإجمالي</td>
                                                <td width="50%" align="right" style="padding:16px 20px; font-size:20px; font-weight:bold; color:{{Gold}}; text-align:right;">{{order.TotalPrice:N0}} دج</td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>

                                <!-- Delivery details -->
                                <tr>
                                    <td align="right" style="padding:0 32px 32px;">
                                        <div align="right" style="font-size:14px; font-weight:bold; color:{{Teal}}; margin-bottom:8px; text-align:right;">عنوان التوصيل</div>
                                        <p align="right" style="font-size:14px; line-height:1.8; color:{{TextDark}}; margin:0; text-align:right;">
                                            {{order.ShippingAddress}}<br />
                                            {{order.Commune}}، {{order.Wilaya}}<br />
                                            {{order.Phone}}
                                        </p>
                                    </td>
                                </tr>

                                <!-- Footer -->
                                <tr>
                                    <td align="center" style="background-color:{{Teal}}; padding:24px;">
                                        <p style="font-size:13px; color:{{Parchment}}; margin:0;">
                                            شكراً لثقتكم بالمكتبة الإسلامية
                                        </p>
                                    </td>
                                </tr>

                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
            """;
        }

        private static string BuildItemsRows(List<OrderItem> items)
        {
            var sb = new StringBuilder();
            foreach (var item in items)
            {
                sb.Append($$"""
                <tr>
                    <td align="right" style="padding:12px 0; border-bottom:1px solid {{Parchment}}; font-size:14px; color:{{TextDark}}; text-align:right;">{{item.ProductName}}</td>
                    <td align="center" style="padding:12px 0; border-bottom:1px solid {{Parchment}}; font-size:14px; color:{{TextDark}}; text-align:center;">{{item.Quantity}}</td>
                    <td align="right" style="padding:12px 0; border-bottom:1px solid {{Parchment}}; font-size:14px; color:{{TextDark}}; text-align:right;">{{item.FullPrice:N0}} دج</td>
                </tr>
                """);
            }
            return sb.ToString();
        }
    }
}
