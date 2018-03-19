using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using TS.Service.DataAccess.Dictionaries;

namespace TS.Web.Models.SuperAdmin
{
    public enum DictionaryType
    {
        [Description(DictionaryService.commentProfession)]
        CommentProfession = 1,
        [Description(DictionaryService.drawingCatalog)]
        DrawingCatalog = 2,
        [Description(DictionaryService.drawingProfession)]
        DrawingProfession = 3,
        [Description(DictionaryService.integralityCommentType)]
        IntegralityCommentType = 4,
        [Description(DictionaryService.designCommentType)]
        DesignCommentType = 5,
        [Description(DictionaryService.totalScore)]
        TotalScore = 6,
        [Description(DictionaryService.passScore)]
        PassScore = 7,
        [Description(DictionaryService.scoreCoefficient)]
        ScoreCoefficient = 8,
        [Description(DictionaryService.commentLevel)]
        CommentLevel = 9,

        /// <summary>
        /// 用于添加一级意见类型，完整性意见和分专业意见
        /// </summary>
        FirstCommentType = 20,
    }
}