﻿import { AppComponent } from '../app.component';
import { Http } from '@angular/http';
import { Observable } from 'rxjs/Rx';
import 'rxjs/add/operator/map';
import { Strategy } from './Strategy';

export class AssetAllocation extends Strategy {

    public assets: string = "MDY,ILF,FEZ,EEM,EPP,VNQ,TLT";
    public assetsConstantLeverage: string = "";             // "1,1,1,-1,1.5,2,2";
    public rebalancingFrequency: string = "Weekly,Fridays";   // "Daily, 2d"(trading days),"Weekly, Fridays", "Monthly, T+1"/"Monthly, T-1" (first/last trading day of the month), 
    public pctChannelLookbackDays: string = "30-60-120-252";
    public pctChannelPctLimits: string = "30-70";
    public pctChannelIsConditional: string = "";            // "Yes"
    public histVolLookbackDays: string = "20";
    public dynamicLeverageClmtParams: string = "";          // "SMA(SPX,50d,200d); PR(XLU,VTI,20d)";   // SPX 50/200 crossover; PR=PriceRatio of XLU/VTI for 20 days
    public uberVxxEventsParams: string = "";                // "FOMC;Holidays"  */

    constructor(p_app: AppComponent) {
        super("AssetAllocation", p_app);
        this.SetParams("idParamSetTAA_VaradiOriginal");     // temporary for Development
    }

    IsMenuItemIdHandled(p_subStrategyId: string): boolean {
        return p_subStrategyId == "idMenuItemTAA";
    }

    GetHtmlUiName(p_subStrategyId: string): string {     // go to HTML UI
        return "Tactical Asset Allocation (TAA)";
    }

    GetTradingViewChartName(p_subStrategyId: string): string {     // go to HTML UI
        return "Asset Allocation";
    }

    GetWebApiName(p_subStrategyId: string): string {
        return "TAA";
    }

    GetHelpUri(p_subStrategyId: string): string {     // go to HTML UI as gDoc URL
        return "https://docs.google.com/document/d/1onl2TKr-8RJqlQjsdIcN6X55fSK-4LO9k5T0Ts79-WU";
    }

    GetStrategyParams(p_subStrategyId: string): string {
        return "&Assets=" + this.assets + "&AssetsConstantLeverage=" + this.assetsConstantLeverage
            + "&RebalancingFrequency=" + this.rebalancingFrequency + "&PctChannelLookbackDays=" + this.pctChannelLookbackDays
            + "&PctChannelPctLimits=" + this.pctChannelPctLimits + "&PctChannelIsConditional=" + this.pctChannelIsConditional
            + "&HistVolLookbackDays=" + this.histVolLookbackDays + "&DynamicLeverageClmtParams=" + this.pctChannelIsConditional
            + "&UberVxxEventsParams=" + this.uberVxxEventsParams;
    }


    public MenuItemParamSetsClicked(event) {
        console.log("MenuItemParamSetsClicked()");
        var target = event.target || event.srcElement || event.currentTarget;
        var idAttr = target.attributes.id;
        var idValue = idAttr.nodeValue;
        this.SetParams(idValue);
    }


    public SetParams(idValue) {
        switch (idValue) {
            case "idParamSetTAA_GlobVnqIbb_WF":
                this.assets = "MDY,ILF,FEZ,EEM,EPP,VNQ,IBB,TLT"
                this.assetsConstantLeverage = "";
                this.rebalancingFrequency = "Weekly,Fridays"
                this.pctChannelLookbackDays = "30-60-120-252";
                this.pctChannelPctLimits = "30-70";
                this.pctChannelIsConditional = "";
                this.histVolLookbackDays = "20";
                this.dynamicLeverageClmtParams = "";
                this.uberVxxEventsParams = "";
                break;
            case "idParamSetTAA_GlobVnq_WF":
                this.assets = "MDY,ILF,FEZ,EEM,EPP,VNQ"
                this.assetsConstantLeverage = "";
                this.rebalancingFrequency = "Weekly,Fridays"
                this.pctChannelLookbackDays = "30-60-120-252";
                this.pctChannelPctLimits = "25-75";
                this.pctChannelIsConditional = "";
                this.histVolLookbackDays = "20";
                this.dynamicLeverageClmtParams = "";
                this.uberVxxEventsParams = "";
                break;
            case "idParamSetTAA_VaradiOriginal":
                this.assets = "VTI,ICF,LQD,DBC"     // Varadi uses SHY (1-3 Year Treasury Bond) for Cash, but Cash is fine. SHY = 0.47% CAGR only, not even half percent
                this.assetsConstantLeverage = "";
                this.rebalancingFrequency = "Monthly,T-1"
                this.pctChannelLookbackDays = "60-120-180-252";
                this.pctChannelPctLimits = "25-75";
                this.pctChannelIsConditional = "";
                this.histVolLookbackDays = "20";
                this.dynamicLeverageClmtParams = "";
                this.uberVxxEventsParams = "";
                break;
            default:
                //this.etf1 = "TVIX";
                //this.weight1 = "-35";      // %, negative is Short
                //this.etf2 = "TMV";
                //this.weight2 = "-65";      // %, negative is Short
                break;
        }
    }   // SetParams()

}
