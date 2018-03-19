/**
 * Created by Pactera on 2018/1/12.
 */


function setDivCenter(divName){
   /* var top = ($(window).height() - $(divName).innerHeight())/2;
    var left = ($(window).width() - $(divName).innerWidth())/2;
    var scrollTop = $(document).scrollTop();
    var scrollLeft = $(document).scrollLeft();
    $(divName).css( { 'top' : top + scrollTop, 'left' : left + scrollLeft } ).show();*/
    $(divName).show();
    $("body").css({
        'overflow':'hidden'
    });
}

function popUp(dom,popDom) {
    $("body").on("click", dom,function () {
        popUpEle(popDom);
    });
}

function popUpEle(popDom) {
        $(".wrap").show();
        setDivCenter(popDom);
        $("body").css({
            'overflow': 'hidden'
        });
}


function errPromot(msg) {
    var mask = $('.err-prompt-wrap');
    var errPrompt = $('.err-prompt');
    mask.fadeIn();
    errPrompt.html(msg).fadeIn();
    var timer = setTimeout(function () {
        errPrompt.hide();
        mask.hide();
        clearTimeout(timer);
    }, 3000);
    mask.off('click').on('click', function () {
        clearTimeout(timer);
        errPrompt.hide();
        mask.hide();
    });
}

function operatePrompt(con, uri,callback) {
    $(".wrap").show();
    setDivCenter(".operatePrompt");
    $("body").css({
        'overflow': 'hidden'
    });
    $(".operatePrompt .title").html(con);
    $(".operatePrompt .confirm-btn").removeClass("sent");
    $(".operatePrompt .confirm-btn").unbind();
    $(".operatePrompt .confirm-btn").attr("data-uri", uri);
    $(".operatePrompt").on("click", ".confirm-btn", function () {
        $(".wrap").hide();
        $(".common-popUp").hide();
        $(".audit-status-popup").hide();
        $("body").css({
            'overflow': 'auto'
        });
    });
    $(".operatePrompt").on("click", ".confirm-btn", function () {
        if ($(this).hasClass("sent"))
            return false;

        $(this).addClass("sent");
        $.ajax({
            async: false,
            type: "POST",
            url: $(this).attr("data-uri"),
            success: callback,
            error: function () {
                errPromot("操作失败");
            },
            beforeSend: function () {
                loading();
            },
            complete: function () {
                loadingSuccess();
            }
        });
    });
}

function  words(dom) {
    var textarea = $(dom).find("textarea");
    var span =  $(dom).find(".words span");
    textarea.on("input", function () {
        var length = this.value.length;
        if (length > 200) {
            span.css("color"," #FF0000");
            this.value = this.value.slice(0,200);
        } else {
            span.html(length);
        }
    });
}


