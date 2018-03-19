/**
 * Created by Pactera on 2017/12/18.
 */
$(function () {

   // var height =  $(document).innerHeight()-89;
   // $(".reviewCompany-content").css("min-height",height);
    //$(".views-box").css("min-height",height);
   // $(".professionalAuditList-content").css("min-height",height);
  //  $(".adminInformationManagement-content").css("min-height",height);
   // $(".scoreEvalution-content").css("min-height",height);
   // $(".product-lists-box").css("min-height",height);
   // $(".admin-project-overview").css("min-height",height);
   // $(".personalInformation-content").css("min-height",height);
   // $(".paperVersionManagement-content").css("min-height",height);
   // $(".enterpriseInformation-content").css("min-height",height);
// $(".content-box").css("min-height",height);
   // $(".sign-box").css("min-height",height);
   // $(".evaluation-box").css("min-height",height);

    //关闭弹窗
    $("body").on("click", ".popUp .close-icon", function () {
        popUpHide();
    });
    $("body").on("click", ".popUp .cancle-btn", function () {
        popUpHide();
    });
    $(".wrap").on("click", function () {
        popUpHide();
    });

    $(".releaseNotes-popUp").on("click", ".confirm-btn", function () {
        popUpHide();
    });


    //切换到个人中心
    $(".HJ-header").on("click", ".personal-center .name", function (e) {
        e.stopPropagation();
        var dropDownBox = $(".user-dropDown-box");
        var personalName = $(".personal-center .name i");
        if (dropDownBox.hasClass("active")) {
            dropDownBox.removeClass("active");
            personalName.css("color", "#fff");

        } else {
            dropDownBox.addClass("active");
            personalName.css("color", "#094C82");

        }
    });
   $(document).on("click", function(e) {
       e.stopPropagation();
       $(".user-dropDown-box").removeClass("active");
       $(".personal-center .name i").css("color", "#fff");
       $(".select-lists").removeClass("active");
       $(".topName i").css("color", "#fff");
   });

    //下拉选择
    function select(select) {
        $("body").on("click",select+" .name",function (e) {
            e.stopPropagation();
            $(".user-dropDown-box").removeClass("active");
            var selectLists = $(this).siblings(".select-lists");
            if (selectLists.hasClass("active")) {
                selectLists.removeClass("active");
            } else {
                selectLists.addClass("active");
            }
        });
        $("body").on("click",select+" .select-lists li",function (e) {
            e.stopPropagation();
            $(this).addClass("active").siblings().removeClass("active");
            var html = $(this).text();
            $(this).parents(".select-lists").siblings(".name").children("span").text(html);
            $(this).parents(".select-lists").removeClass("active");
        });
    }
    select(".drawingStatus");
    select(".modelStatus");
    select(".selectFloor-con");
    select(".select-con");


   /* $(".imprint div").each(function(index ,val) {
        var text = $(this).text();
        var len = text.length;
        if(len> 27) {
            $(this).text(text.slice(0,27)+"...");
        } else {
            $(this).text(text);
        }
        if(len> 15) {
            $(this).css("text-align","left");
        }
    });*/
});


function popUpHide() {
    $(".wrap").hide();
    $(".common-popUp").hide();
    $(".audit-status-popup").hide();
    $(".new-opinion-content").hide();
    $("body").css({
        'overflow': 'auto'
    });
}