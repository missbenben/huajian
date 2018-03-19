$(window).ready(function () {
    ////隐藏下部工具栏
    //window.api.userEventService.hideViewToolbar.emit(true);
    ////隐藏右侧工具栏
    //window.api.userEventService.hideRightSpaceTree.emit(true);
    //添加批注成功后返回的信息
    window.api.userEventService.addAnnotation.subscribe(function (res) {
        console.log(res);

        //在父页面添加图片dom，存入snapshot和id
        var html = '<li class="markList"><a href="javascript:void(0);" onclick="viewAnnotation(\'' + res.ID + '\');"><img src="' + res.Snapshot + '" alt="' + res.ID + '" class="mark-img"></a><img src="/Content/image/ICON_DELETE.png" alt="" class="delete"></li>';
        window.parent.$(html).insertBefore('#addAnnotation');
        window.parent.$(".wrap").show();
        window.parent.$(".new-opinion-content").show();
        window.parent.addNewCommentModel.BIMElements.push({
            Id: "0",
            BIMElementType: "0",
            IsDelete: "0",
            BIMId: res.ID,
            BimThumb: res.Snapshot
        });
        //window.parent.$('#newAnnotation').append(html);
        window.parent.loadingSuccess();

    });
    //删除批注成功后返回的信息
    window.api.userEventService.deleteAnnotation.subscribe(function (viewpoint) {
        //window.api.userService.getViewpoint(viewpoint.ID).then(function(res) {
        console.log(viewpoint);
        //});
    });
    
});
