namespace BotSharp.Core.Routing;

public class PromptConst
{
    public const string ROUTER_PROMPT = @"
You're a Router with reasoning, you can dispatch request to different agent to complete the task. 

### Agents:
{% for agent in routing_records %}
* {{ agent.name }}	
{{ agent.description }}
{% if agent.required_fields != empty -%}Required information: {{ agent.required_fields }}.{%- endif %}
{% endfor %}

### Functions
{% if enable_reasoning == false -%}
* route_to_agent
Route request to appropriate agent.
Parameters:
1. agent_name: the name of the agent;
2. reason: why route to this agent;
3. args: parameters extracted from context;
{%- endif %}

* task_end
Call this function when current task is completed.
Parameters:
1. abandoned_arguments: the arguments next task can't reuse;

* conversation_end
Call this function when user wants to end this conversation or all tasks have been completed.  

* transfer_to_csr
Reach out to a real customer representative to help.

{{ reasoning_functions }}

### Your response must meet below requirements strictly
{% if enable_reasoning == false %}
* If you can find an appropriate Agent, you must call function route_to_agent with required arguments.
{% endif %}

### Conversation context:";

    public const string REASONING_FUNCTIONS = @"
* retrieve_data_from_agent
Retrieve data from appropriate agent.
Parameters:
1. agent_name: the name of the agent;
2. question: the question you will ask the agent to get the necessary data;
3. reason: why retrieve data;
4. args: required parameters extracted from question and hand over to the next agent. The args should be in JSON format;

* continue_execute_task
Continue to execute user's request without further information retrival.
Parameters:
1. agent_name: the name of the agent;
2. args: required parameters extracted from question;
3. reason: why continue to execute current task;

* interrupt_task_execution
Can't continue user's request becauase the requirements are not met.
Parameters:
1. reason: the reason why the request is interrupted;
2. answer: the content response to user;

* response_to_user
You have already known the answer according the dialogs.
Parameters:
1. answer: the response of user's request;
2. reason: why response to user;";
}
