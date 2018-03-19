/**
 * Created by Pactera on 2018/1/12.
 */
//loading 状态
function loading() {
    $(".loadingWrap").show();
    $(".Loading").show();
}
//loading结束
function loadingSuccess() {
    $(".loadingWrap").hide();
    $(".Loading").hide();
}

//接口请求时候，封装自动弹出的loading，以及关闭，url, method, success  必须传入
function Ajax(url, method, success, data, failed) {
    loading();
    if (!data)
        data = null;
    var request = $.ajax({
        url: url,
        method: method,
        data: data,
        dataType: "json"
    });

    request.done(function (msg) {
        success(msg);
        loadingSuccess();
    });

    request.fail(function (jqXHR, textStatus) {
        if (failed)
            failed(jqXHR, textStatus);
        loadingSuccess();
    });
}