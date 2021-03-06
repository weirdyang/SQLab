import { Component, Inject, OnInit, AfterViewInit } from '@angular/core';
import { Http } from '@angular/http';
import { Observable } from 'rxjs/Observable';
import { Strategy } from './Strategies/Strategy'
import { VXX_SPY_Controversial } from './Strategies/VXX_SPY_Controversial'
import { LEtf, AngularInit_LEtf } from './Strategies/LEtf'
import { TotM } from './Strategies/TotM'
import { AdaptiveUberVxx } from './Strategies/AdaptiveUberVxx'
import { AssetAllocation } from './Strategies/AssetAllocation'
import { MomTF } from './Strategies/MomTF'
import { StopWatch } from './Utils'

declare var TradingView: any;
//declare var TradingViewWidget: any;
declare var Datafeeds: any;
declare var gSqUserEmail: string;
declare var gTradingViewChartOnreadyCalled: boolean;
declare var jquery: any;
declare var $: any;

declare var chartWidget: any;

class DailyBar {
    public time: number = 0;  // gives back the miliseconds, so it is OK.  //time: data.t[i] * 1000,
    public close: number = 0.0;
    public open: number = 0.0;
    public high: number = 0.0;
    public low: number = 0.0;

}

@Component({
    selector: 'quicktester',
    templateUrl: './quicktester.component.html',
    styleUrls: ['./quicktester.component.css']
})
export class QuickTesterComponent {
    public m_userEmail: string = 'Unknown user';

    public m_versionShortInfo: string = "v0.2.32";    // strongly typed variables in TS
    public versionLongInfo: string = "SQ QuickTester  \nVersion 0.2.32  \nDeployed: 2016-11-04T21:00Z";  // Z means Zero UTC offset, so, it is the UTC time, http://en.wikipedia.org/wiki/ISO_8601
    public tipToUser: string = "Select Strategy and press 'Start Backtest'...";
    public tradingViewChartWidget: any = null;

    public tradingViewChartName: string = "DayOfTheWeek data";

    public inputStartDateStr: string = "";  // empty string means maximum available
    public inputEndDateStr: string = "";    // empty string means: today

    // Inputs area
    public strategy_LEtf: LEtf = <LEtf>{};     // strategy variables are needed separately, because HTML uses them
    public strategy_VXX_SPY_Controversial: VXX_SPY_Controversial = <VXX_SPY_Controversial>{};
    public strategy_TotM: TotM = <TotM>{};
    public strategy_AdaptiveUberVxx: AdaptiveUberVxx = <AdaptiveUberVxx>{};
    public strategy_AssetAllocation: AssetAllocation = <AssetAllocation>{};
    public strategy_MomTF: MomTF = <MomTF>{};

    public strategies: Strategy[] = [];
    public selectedStrategy: Strategy = <Strategy>{};;      // Identifies the main strategy, but not the sub-strategy. 
    public selectedSubStrategyMenuItemId = ""; // This identifies the substrategy under Strategy.  Also, the HTML hidden or visible parts are controlled by this. 
    public selectedSubStrategyName = "";
    public selectedSubStrategyHelpUri = "";

    public profilingBacktestStopWatch = null;
    public profilingBacktestCallbackMSec = null;
    public profilingBacktestAtChartReadyStartMSec = null;
    public profilingBacktestAtChartReadyEndMSec = null;

    // Output Statistics area
    public startDateStr: string = "";
    public endDateStr: string = "";
    public rebalanceFrequencyStr: string = "";
    public benchmarkStr: string = "";

    public pvStartValue: number = 1;
    public pvEndValue: number = 1;
    public totalGainPct: number = 1;
    public cagr: number = 1;
    public annualizedStDev: number = 1;
    public sharpeRatio: number = 1;
    public sortinoRatio: number = 1;
    public maxDD: number = 1;
    public ulcerInd: number = 1;// = qMean DD
    public maxTradingDaysInDD: number = 1;
    public winnersStr = 1;
    public losersStr = 1;

    public benchmarkCagr: number = 1;
    public benchmarkMaxDD: number = 1;
    public benchmarkCorrelation: number = 0;

    public pvCash: number = 1;
    public nPositions: number = 0;
    public holdingsListStr: string = "";
    public htmlNoteFromStrategy: string = "";



    public chartDataFromServer = null;  // original, how it arrived from server
    public chartDataInStr = null;   // for showing it in HTML for debug porposes
    //public chartDataToChart = null; 
    public chartDataToChart: DailyBar[] = [];   //// processed: it has time: close, open values, so we have to process it only once
    public nMonthsInTimeFrame: string = "24";
    public startDateUtc: Date = new Date(2000, 0, 1, 1, 0);    // 1st January, 2000, T: 1:00
    public endDateUtc: Date = new Date(2000, 0, 1, 1, 0);    // 1st January, 2000, T: 1:00

    public debugMessage: string = "";
    public errorMessage: string = "";




    constructor(private http: Http) { }

    ngOnInit() {
        console.log("ngOnInit() START");
        //this.getHMData(gDefaultHMData);
        if (typeof gSqUserEmail == "undefined")
            this.m_userEmail = 'undefined@gmail.com';
        else
            this.m_userEmail = gSqUserEmail;

        this.strategy_LEtf = new LEtf(this);
        this.strategy_VXX_SPY_Controversial = new VXX_SPY_Controversial(this);
        this.strategy_TotM = new TotM(this);
        this.strategy_AdaptiveUberVxx = new AdaptiveUberVxx(this);
        this.strategy_AssetAllocation = new AssetAllocation(this);
        this.strategy_MomTF = new MomTF(this);

        this.strategies = [this.strategy_LEtf, this.strategy_VXX_SPY_Controversial, this.strategy_TotM, this.strategy_AdaptiveUberVxx, this.strategy_AssetAllocation, this.strategy_MomTF];
        //this.SelectStrategy("idMenuItemAdaptiveUberVxx"); // there is no #if DEBUG in TS yet. We use TotM rarely in production anyway, so UberVXX can be the default, even while developing it.
        //this.SelectStrategy("idMenuItemTAA");   // temporary default until it is being developed
        this.SelectStrategy("idMenuItemMomTF");   // temporary default until it is being developed
        this.TradingViewChartOnready();
    }

    ngAfterViewInit() {     // equivalent to $(document).ready()
        //Ideally you should wait till component content get initialized, in order to make the DOM available on which you wanted to apply jQuery. For that you need to use AfterViewInit which is one of hook of angular2 lifecycle.
        //here you will have code where component content is ready.
        console.log("ngAfterViewInit() START");
        if (typeof $ == "undefined") { // on server side Index.cshtml is rendered without _Layout.cshtml, so no <HEAD> of HTML is processed, so global variables and even charting_library.min.js is not processed yet.
            console.log("ngAfterViewInit() called. $ is undefined");
            return;
        }      
        console.log("ngAfterViewInit() START 2");
    }

    SelectStrategy(menuItemId: string) {
        this.selectedSubStrategyMenuItemId = menuItemId;

        for (var i = 0; i < this.strategies.length; i++) {
            var strategy = this.strategies[i];
            if (strategy.IsMenuItemIdHandled(menuItemId)) {
                this.selectedStrategy = strategy;
                strategy.OnStrategySelected(menuItemId);
                this.selectedSubStrategyName = strategy.GetHtmlUiName(menuItemId);
                this.selectedSubStrategyHelpUri = strategy.GetHelpUri(menuItemId);
                break;
            }
        }
    }


    TradingViewChartOnready() {
        // on server side rendering <HEAD> of HTML is not processed, so gTradingViewChartOnreadyCalled and even charting_library.min.js is not processed yet.
        if (typeof gTradingViewChartOnreadyCalled == "undefined")
            console.log("TradingViewChartOnready() called by ngOnInit(). Global gTradingViewOnreadyCalled: " + "UNDEFINED");
        else
            console.log("TradingViewChartOnready() called by ngOnInit(). Global gTradingViewOnreadyCalled: " + gTradingViewChartOnreadyCalled);

        if (typeof TradingView == "undefined") {
            console.log("TradingViewChartOnready() called by ngOnInit(). TradingView is undefined");
            return;
        }
        //https://github.com/tradingview/charting_library/wiki/Widget-Constructor
        var widget = new TradingView.widget({
            //fullscreen: true,
            symbol: 'PV',
            //symbol: 'AA',
            interval: 'D',
            container_id: "tv_chart_container",
            //	BEWARE: no trailing slash is expected in feed URL
            datafeed: new Datafeeds.UDFCompatibleDatafeed(this, "http://demo_feed.tradingview.com"),
            library_path: "/charting_library/charting_library/",
            locale: getParameterByName('lang') || "en",
            drawings_access: { type: 'black', tools: [{ name: "Regression Trend" }] },   //	Regression Trend-related functionality is not implemented yet, so it's hidden for a while

            charts_storage_url: 'http://saveload.tradingview.com',
            charts_storage_api_version: "1.1",
            client_id: 'tradingview.com',
            user_id: 'public_user_id'

            , width: "90%"        //Remark: if you want the chart to occupy all the available space, do not use '100%' in those field. Use fullscreen parameter instead (see below). It's because of issues with DOM nodes resizing in different browsers.
            , height: 400
            //https://github.com/tradingview/charting_library/wiki/Featuresets
            //,enabled_features: ["trading_options"]    
            //, enabled_features: ["charting_library_debug_mode", "narrow_chart_enabled", "move_logo_to_main_pane"] //narrow_chart_enabled and move_logo_to_main_pane doesn't do anything to me
            //, enabled_features: ["charting_library_debug_mode"]
            //, disabled_features: ["use_localstorage_for_settings", "volume_force_overlay", "left_toolbar", "control_bar", "timeframes_toolbar", "border_around_the_chart", "header_widget"]
            , disabled_features: ["border_around_the_chart"]
            , debug: true   // Setting this property to true makes the chart to write detailed API logs to console. Feature charting_library_debug_mode is a synonym for this field usage.
            , time_frames: [
                { text: this.nMonthsInTimeFrame + "m", resolution: "D" },   // this can be equivalent to ALL. Just calculate before how many years, or month. DO WORK with months.
                { text: this.nMonthsInTimeFrame + "m", resolution: "W" },   // this can be equivalent to ALL. Just calculate before how many years, or month. DO WORK with months.
                { text: this.nMonthsInTimeFrame + "m", resolution: "M" },   // this can be equivalent to ALL. Just calculate before how many years, or month. DO WORK with months.
                //{ text: "All", resolution: "6M" }, crash: first character should be a Number
                //{ text: "600m", resolution: "D" },   // "600m" 50 years : Put an insanely high value here. But later in the calculateHistoryDepth() we will decrease it to backtested range
                //{ text: "601m", resolution: "D" },   // "601m" 50 years : Put an insanely high value here. But later in the calculateHistoryDepth() we will decrease it to backtested range
                //{ text: "12y", resolution: "D" },   // this can be equivalent to ALL. Just calculate before how many years, or month.
                //{ text: "6000d", resolution: "D" },   // this can be equivalent to ALL. Just calculate before how many years, or month. DO NOT WORK. Max days: 350
                //{ text: "50y", resolution: "6M" },
                //{ text: "3y", resolution: "W" },
                //{ text: "8m", resolution: "D" },
                //{ text: "2m", resolution: "D" }
            ]
            , overrides: {
                "mainSeriesProperties.style": 3,    // area style
                "symbolWatermarkProperties.color": "#644",
                "moving average exponential.length": 13     // but doesn't work. It will be changed later anyway.
            },
        });

        this.tradingViewChartWidget = widget;

        var that = this;
        widget.onChartReady(function () {   // inside this scope: this = widget, that = AppComponent
            console.log("widget.onChartReady()");
            
            var chartWidget = that.tradingViewChartWidget;
            chartWidget.chart().createStudy('Moving Average Exponential', false, false, [26]);     //inputs: (since version 1.2) an array of study inputs.

            // Decision: don't use setVisibleRange(), because even if we set up EndDate as 5 days in the future, it cuts the chart until 'today' sharp.
            // leave the chart as default, that gives about 5 empty days in the future, which we want. Which looks nice.
            //if (that.endDateUtc != null) {
            //    var visibleRangeStartDateUtc = new Date();
            //    visibleRangeStartDateUtc.setTime(that.endDateUtc.getTime() - 365 * 24 * 1000 * 60 * 60);       // assuming 365 calendar days per year, set the visible range for the last 1 year
            //    visibleRangeStartDateUtc.setHours(0, 0, 0, 0);
            //    var visibleRangeEndDateUtc = new Date();
            //    visibleRangeEndDateUtc.setTime(that.endDateUtc.getTime() + 5 * 24 * 1000 * 60 * 60);       // it is nice (and by default) the visible endDate is about 5 days in the future (after today)
            //    visibleRangeEndDateUtc.setHours(0, 0, 0, 0);
            //    if (visibleRangeStartDateUtc < visibleRangeEndDateUtc) {
            //        console.log("widget.onChartReady(): chart().setVisibleRange()");

            //        var oldVisibleRange = that.tradingViewChartWidget.chart().getVisibleRange();       
            //        // getVisibleRange gives back Wrong time range: {"from":1459814400,"to":0} but 
            //        // setVisibleRange doesn't accept that: gives back to Console: "Wrong time range: {"from":1459814400,"to":0} ". So it is buggy.

            //        that.tradingViewChartWidget.chart().setVisibleRange({       // this was introduced per my request: https://github.com/tradingview/charting_library/issues/320
            //            from: visibleRangeStartDateUtc.getTime() / 1000,
            //            to: oldVisibleRange.to                                      // if we give '0', it says: "Wrong time range:
            //            //to: visibleRangeEndDateUtc.getTime() / 1000               // if we give 'today + 5 days' in the future, it still cuts the chart at today. Bad.
            //        });
            //    }
            //}
        });

    }   // TradingViewChartOnready()



    MenuItemStartBacktestClicked() {
        console.log("MenuItemStartBacktestClicked() START");

        let generalInputParameters: string = "StartDate=" + this.inputStartDateStr + "&EndDate=" + this.inputEndDateStr;
        this.selectedStrategy.StartBacktest(this.http, generalInputParameters, this.selectedSubStrategyMenuItemId);
        //this.profilingBacktestStopWatch = new StopWatch();
        //this.profilingBacktestStopWatch.Start();
    }


    MenuItemVersionInfoClicked() {
        alert(this.versionLongInfo);
    }



    ProcessStrategyResult(strategyResult: any) {
        console.log("ProcessStrategyResult() START");

        //this.profilingBacktestCallbackMSec = this.profilingBacktestStopWatch.GetTimestampInMsec();

        if (strategyResult.errorMessage != "") {
            alert(strategyResult.errorMessage);
            return; // in this case, don't do anything; there is no real Data.
        }

        this.startDateStr = strategyResult.startDateStr;
        this.rebalanceFrequencyStr = strategyResult.rebalanceFrequencyStr;
        this.benchmarkStr = strategyResult.benchmarkStr;

        this.endDateStr = strategyResult.endDateStr;
        this.pvStartValue = strategyResult.pvStartValue;
        this.pvEndValue = strategyResult.pvEndValue;
        this.totalGainPct = strategyResult.totalGainPct;
        this.cagr = strategyResult.cagr;
        this.annualizedStDev = strategyResult.annualizedStDev;
        this.sharpeRatio = strategyResult.sharpeRatio;
        this.sortinoRatio = strategyResult.sortinoRatio;
        this.maxDD = strategyResult.maxDD;
        this.ulcerInd = strategyResult.ulcerInd;
        this.maxTradingDaysInDD = strategyResult.maxTradingDaysInDD;
        this.winnersStr = strategyResult.winnersStr;
        this.losersStr = strategyResult.losersStr;

        this.benchmarkCagr = strategyResult.benchmarkCagr;
        this.benchmarkMaxDD = strategyResult.benchmarkMaxDD;
        this.benchmarkCorrelation = strategyResult.benchmarkCorrelation;

        this.pvCash = strategyResult.pvCash;
        this.nPositions = strategyResult.nPositions;
        this.holdingsListStr = strategyResult.holdingsListStr;

        this.htmlNoteFromStrategy = strategyResult.htmlNoteFromStrategy;
        var htmlElementNote = document.getElementById("idHtmlNoteFromStrategy");
        if (htmlElementNote != null)
            htmlElementNote.innerHTML = strategyResult.htmlNoteFromStrategy;


        // 2019-10-08: in Edge it was OK, but this caused Chrome: "Aw, Snap", "Debugging connection was closed. Reason. Render process gone."
        // This is because all the HarryLong weights for all ETFs daily were sent in the 'debugMessage'. Sending is not a problem. But it was a HTML data, and Chrome crashed while trying to visualize, build up a DOM tree. 
        // Probably because there is a threshold for max number of new HTML elements.
        // Interestingly, Edge worked.
        if (strategyResult.debugMessage.length > 10000) {
            console.log("SqWarn! Too long debugMessage. Above approx 100K, it would crash Chrome (although Edge survive). Consider decreasing debugMessage size. DebugMessage is: ");
            console.log(strategyResult.debugMessage);
        } else {
            this.debugMessage = strategyResult.debugMessage;
        }

        this.errorMessage = strategyResult.errorMessage;


        this.chartDataFromServer = strategyResult.chartData;

        this.chartDataToChart = [];
        var prevDayClose = -1;
        for (var i = 0; i < strategyResult.chartData.length; i++) {
            var rowParts = strategyResult.chartData[i].split(",");
            var dateParts = rowParts[0].split("-");
            var dateUtc = new Date(Date.UTC(parseInt(dateParts[0]), parseInt(dateParts[1]) - 1, parseInt(dateParts[2]), 0, 0, 0));

            var closePrice = parseFloat(rowParts[1]);
            var openPrice = (i == 0) ? closePrice : prevDayClose;
            var barValue : DailyBar = {
                time: dateUtc.getTime(),  // gives back the miliseconds, so it is OK.  //time: data.t[i] * 1000,
                close: closePrice,
                open: openPrice,
                high: (i == 0) ? closePrice : ((openPrice > closePrice) ? openPrice : closePrice),
                low: (i == 0) ? closePrice : ((openPrice < closePrice) ? openPrice : closePrice)
            }

            prevDayClose = barValue.close;
            this.chartDataToChart.push(barValue);
        }

        // calculate number of months in the range
        this.startDateUtc = new Date(this.chartDataToChart[0].time);
        this.endDateUtc = new Date(this.chartDataToChart[this.chartDataToChart.length - 1].time);
        var nMonths = (this.endDateUtc.getFullYear() - this.startDateUtc.getFullYear()) * 12;
        nMonths -= this.startDateUtc.getMonth() + 1;
        nMonths += this.endDateUtc.getMonth();
        nMonths = nMonths <= 0 ? 1 : nMonths;   // if month is less than 0, tell the chart to have 1 month

        this.chartDataInStr = strategyResult.chartData.reverse().join("\n");            // This writes down the big "+ Show debug info" part. Maybe too much text.

        this.nMonthsInTimeFrame = nMonths.toString();

        //////***!!!!This is the best if we have to work with the official Chart, but postMessage works without this
        //////  Refresh TVChart (make it call the getBars()), version 2: idea stolen from widget.setLangue() inner implementation. It will redraw the Toolbars too, not only the inner area. But it can change TimeFrames Toolbar
        // this part will set up the Timeframes bar properly, but later is chart.onChartReady() you have to click the first button by "dateRangeDiv.children['0'].click();"
        if ((this.tradingViewChartWidget != null) && (this.tradingViewChartWidget._ready != null) && (this.tradingViewChartWidget._ready == true)) {
            // we have 2 options: 1. or 2. to refresh chart after data arrived:
            // 1. update chart without recreating the whole chartWidget. This would be smooth and not blink.
            // setVisibleRange() nicely works, by our request, but the time_frames[] are not updated. So, it is not ideal. So, choose to remove and recreate the chart instead.
            //this.tradingViewChartWidget.options.time_frames[0].text = nMonths + "m";
            //this.tradingViewChartWidget.options.time_frames[1].text = nMonths + "m";
            //this.tradingViewChartWidget.options.time_frames[2].text = nMonths + "m";
            //this.tradingViewChartWidget.removeAllStudies();
            //this.tradingViewChartWidget.setSymbol("PV", 'D');
            //this.tradingViewChartWidget.chart().setVisibleRange({       // this was introduced per my request: https://github.com/tradingview/charting_library/issues/320
            //    from: Date.UTC(2012, 2, 3) / 1000,
            //    to: Date.UTC(2015, 3, 3) / 1000
            //});
            //this.tradingViewChartWidget.createStudy('Moving Average Exponential', false, false, [26]);

            // 2. Update the chart with recreating the whole frame. This will blink, as the frame part will disappear. However, it is quite quick, so it is ok.
            this.tradingViewChartWidget.remove();       // this is the way to the widget.options to be effective
            ////gTradingViewChartWidget.options.time_frames[0].text = "All";    // cannot be "All"; it crashes.
            this.tradingViewChartWidget.options.time_frames[0].text = nMonths + "m";
            this.tradingViewChartWidget.options.time_frames[1].text = nMonths + "m";
            this.tradingViewChartWidget.options.time_frames[2].text = nMonths + "m";
            ////gTradingViewChartWidget.options.width = "50%";        // works too in Remove(), Create()
            this.tradingViewChartWidget.create()
        }
        console.log("ProcessStrategyResult() END");
    }



    onHeadProcessing() {
        console.log('onHeadProcessing()');
    }


    MenuItemStrategyClick(event: any) {
        console.log("MenuItemStrategyClick() START");

        $(".sqMenuBarLevel2").hide();
        $(".sqMenuBarLevel1").hide();

        var target = event.target || event.srcElement || event.currentTarget;
        var idAttr = target.attributes.id;
        var value = idAttr.nodeValue;
        this.SelectStrategy(value);
    }



    SQToggle(hiddenTextID: any, alwaysVisibleSwitchID: any, switchDisplayText: any) {
        console.log("SQToggle() START");

        var hiddenText = document.getElementById(hiddenTextID);
        var switchElement = document.getElementById(alwaysVisibleSwitchID);
        if (hiddenText != null && switchElement != null)
            if (hiddenText.style.display == "block") {
                hiddenText.style.display = "none";
                switchElement.innerHTML = "+ Show " + switchDisplayText;
            } else {
                hiddenText.style.display = "block";
                switchElement.innerHTML = "- Hide " + switchDisplayText;
            }
    }

    OnParameterInputKeypress(event: any) {
        var chCode = ('charCode' in event) ? event.charCode : event.keyCode;
        if (chCode == 13)
            this.MenuItemStartBacktestClicked();
        //alert("The Unicode character code is: " + chCode);
    }


    // debug info here
    m_webAppResponse: string = "";
    m_wasRefreshClicked: any;

    clickMessage = '';
    onClickMe() {
        this.clickMessage = 'You are my hero!';
    }

}





// ************** Utils section with Global functions

function getParameterByName(name: string) {         // copyed from Tradingview's Test.html
    name = name.replace(/[\[]/, "\\[").replace(/[\]]/, "\\]");
    var regex = new RegExp("[\\?&]" + name + "=([^&#]*)"),
        results = regex.exec(location.search);
    return results === null ? "" : decodeURIComponent(results[1].replace(/\+/g, " "));
}


