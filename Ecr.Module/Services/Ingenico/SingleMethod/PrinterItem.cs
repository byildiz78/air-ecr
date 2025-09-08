using Ecr.Module.Services.Ingenico.GmpIngenico;
using Ecr.Module.Services.Ingenico.Helper;
using Ecr.Module.Services.Ingenico.Models;
using System;

namespace Ecr.Module.Services.Ingenico.SingleMethod
{
    public class PrinterItem
    {
        public static GmpPrepareDto PrepareItemSale(FiscalOrderLine orderline)
        {
            ushort currency = 949;
            GmpPrepareDto _prepare = new GmpPrepareDto();
            try
            {
                //Adet 0 dan büyükse normal satır,değilse iptal satırı -
                if (orderline.SaleQuantity > 0)
                {
                    //var IsPromotion = false;
                    //if (!string.IsNullOrEmpty(orderline.PromotionCode) && orderline.SaleQuantity > 1)
                    //{
                    //    orderline.Barcode = orderline.SaleQuantity + " Adt X " + orderline.UnitPrice;
                    //    orderline.UnitPrice = Math.Round(orderline.UnitPrice.Value * orderline.SaleQuantity.Value, 2);
                    //    orderline.SaleQuantity = 1;
                    //    IsPromotion = true;
                    //}


                    ST_TICKET m_stTicket = new ST_TICKET();
                    ST_ITEM Item = new ST_ITEM();
                    Item.type = Defines.ITEM_TYPE_DEPARTMENT;
                    Item.subType = 0;

                   
                    orderline.ProductName = orderline.ProductName; // Regex.Replace(orderline.ProductName, "[^0-9a-zA-Z]+", "");

                    Item.deptIndex = (byte)(Convert.ToInt32(orderline.TaxGroupID) - 1);
                    Item.name = orderline.ProductName.Replace("\"", "").Replace("'", "");
                    Item.amount = SettingsValues.receiptWeightNotDetails == "1" && orderline.UnitName.ToUpper().Equals("KG") ?
                        (uint)CommandHelperGmpProvider.DoubleFormat(orderline.UnitPrice.Value * orderline.SaleQuantity.Value, 2) :
                        (uint)CommandHelperGmpProvider.DoubleFormat(orderline.UnitPrice.Value, 2);

                    Item.currency = currency;

                    Item.count =
                        orderline.UnitName.ToUpper().Equals("KG") && SettingsValues.receiptWeightNotDetails == "1" ?
                        (uint)CommandHelperGmpProvider.DoubleFormat(1, 0) :
                         orderline.UnitName.ToUpper().Equals("KG") ?
                        (uint)CommandHelperGmpProvider.DoubleFormat(orderline.SaleQuantity.Value, 3) :
                        orderline.SaleQuantity.Value % 1 == 0 ?
                        (uint)CommandHelperGmpProvider.DoubleFormat(orderline.SaleQuantity.Value, 0) :
                        (uint)CommandHelperGmpProvider.DoubleFormat(orderline.SaleQuantity.Value, 3);

                    Item.unitType =
                        orderline.UnitName.ToUpper().Equals("KG") && SettingsValues.receiptWeightNotDetails == "1" ?
                        (byte)EItemUnitTypes.ITEM_NONE :
                        orderline.UnitName.ToUpper().Equals("KG") ?
                        (byte)EItemUnitTypes.ITEM_KILOGRAM :
                        (byte)EItemUnitTypes.ITEM_NONE;


                    Item.pluPriceIndex = 0;
                    Item.countPrecition = (byte)(
                        orderline.UnitName.ToUpper().Equals("KG") && SettingsValues.receiptWeightNotDetails == "1" ?
                        0 :
                        orderline.UnitName.ToUpper().Equals("KG") ?
                        3 :
                        orderline.SaleQuantity % 1 == 0 ? 0 : 3);

                    //(byte)((orderline.UnitName.ToUpper().Equals("KG")) ? 
                    //    SettingsValues.receiptWeightNotDetails == "1" ?  
                    //    0 : 
                    //    3 : (orderline.SaleQuantity.Value % 1 == 0 ? 0 : 3));

                    //(byte)((transaction.UnitName.ToUpper().Equals("KG")) ? 3 : (transaction.Quantity % 1 == 0 ? 0 : 3));
                    Item.barcode = SettingsValues.barcode == "1" ? orderline.Barcode : "";

                    //Item.flag |= (uint)EItemOptions.ITEM_TAX_EXCEPTION_VAT_INCLUDED_TO_PRICE;

                    //Promosyon kullanıldı ise 
                    //if (!string.IsNullOrEmpty(orderline.PromotionCode))
                    //{
                    //    Item.promotion = new promotion();
                    //    Item.promotion.amount = (int)CommandHelperGmpProvider.DoubleFormat(orderline.PromotionAmount.Value, 2);
                    //    Item.promotion.type = (byte)EItemPromotionType.ITEM_PROMOTION_DISCOUNT;
                    //    Item.promotion.ticketMsg = orderline.PromotionName;
                    //}
                   // LogManager.Append($"Satılan ürün =>{Newtonsoft.Json.JsonConvert.SerializeObject(Item)}");
                    _prepare.bufferLen = Json_GMPSmartDLL.prepare_ItemSale(_prepare.buffer, _prepare.buffer.Length, ref Item);

                }
            }
            catch (Exception ex)
            {
                //LogManager.Exception(ex, "BatchCommadGmpProvider.PrepareItemSale");
            }
            return _prepare;
        }

        public static GmpPrepareDto PrepareVoidItem(FiscalOrderLine orderline)
        {
            GmpPrepareDto _prepare = new GmpPrepareDto();
            uint itemCount = 0;
            byte itemCountPrecition = 0;
            long m_index = 0;
            try
            {
                //ürün iptal satırı  
                var quantity = Math.Abs(orderline.SaleQuantity.Value);
                itemCountPrecition = (byte)(orderline.UnitName.ToUpper().Equals("KG") ? 3 : 0);
                itemCount = orderline.UnitName.ToUpper().Equals("KG")
                    ? (uint)CommandHelperGmpProvider.DoubleFormat(quantity, 3)
                    : (uint)CommandHelperGmpProvider.DoubleFormat(quantity, 0);


                if (orderline.VoidLineID != null)
                {
                    m_index = (long)orderline.VoidLineID;

                    _prepare.bufferLen = GMPSmartDLL.prepare_VoidItem(_prepare.buffer, _prepare.buffer.Length,
                        (ushort)m_index, itemCount, itemCountPrecition);
                }

            }
            catch (Exception ex)
            {
                //LogManager.Exception(ex, "BatchCommadGmpProvider.PrepareVoidItem");
            }
            return _prepare;
        }

        public static GmpPrepareDto PrepareMinus(double discount, int? itemNo = null)
        {
            GmpPrepareDto _prepare = new GmpPrepareDto();
            try
            {
                ushort m_itemNo = 0xFFFF;
                //çek indirim
                if (discount > 0)
                {
                    if (itemNo != null)
                    {
                        m_itemNo = (ushort)itemNo;
                    }
                    _prepare.bufferLen = GMPSmartDLL.prepare_Minus(_prepare.buffer, _prepare.buffer.Length, (int)CommandHelperGmpProvider.DoubleFormat(discount, 2), m_itemNo);
                }
            }
            catch (Exception ex)
            {
                //LogManager.Exception(ex, "BatchCommadGmpProvider.PrepareMinus");
            }
            return _prepare;
        }
    }
}
