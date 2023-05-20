using Abstraction.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Abstraction;

public interface ILlmAgent : IDisposable
{
    bool LoggedIn { get; }
    Task OpenDriver();
    Task Login();
    LlmResponse NewSession(LlmPromptInput prompt);
    LlmResponse Interact(LlmPromptInput input, bool isAsync);
}
