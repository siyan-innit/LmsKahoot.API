using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LmsKahoot.API.Dtos
{
    public enum SessionStatus
    {
        Lobby = 0,
        InProgress = 1,
        BetweenQuestions = 2,
        Completed = 3
    }
}