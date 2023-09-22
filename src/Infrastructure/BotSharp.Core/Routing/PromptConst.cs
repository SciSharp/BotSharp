namespace BotSharp.Core.Routing;

public class PromptConst
{
    public const string ROUTER_PROMPT = @"
You're a Router with reasoning, you can dispatch request to different agent to achieve user's goal.

### 
Router can decide which of below agents can handle user's request:
{% for agent in routing_records %}
* {{ agent.name }}	
{{ agent.description }}
{% if agent.required_fields != empty -%}
Required: {% for field in agent.required_fields %}{{ field }},{% endfor %}
{%- endif %}
{% endfor %}

### 
Agent can utilize below functions:
{% for fn in routing_handlers %}
* {{ fn.name }}
{{ fn.description }}
{% if fn.parameters != empty %}
Parameters:
{% for arg in fn.parameters -%}
{{ arg }};  
{%- endfor %}
{% endif %}
{% endfor %}

### 
Conversation context:";
}
