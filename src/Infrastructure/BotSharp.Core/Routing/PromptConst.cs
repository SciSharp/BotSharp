namespace BotSharp.Core.Routing;

public class PromptConst
{
    public const string ROUTER_PROMPT = @"
You're a Router with reasoning. Follow these steps to handle user's request:
1. Read the CONVERSATION context.
2. Select a appropriate function from FUNCTIONS.
3. Determine which agent from AGENTS is suitable for the current task.

FUNCTIONS
{% for fn in routing_handlers %}
* {{ fn.name }}
{{ fn.description }}
{% if fn.parameters != empty -%}
Parameters:
{% for arg in fn.parameters -%}
{{ arg }};  
{%- endfor %}
{%- endif %}
{% endfor %}

AGENTS
{% for agent in routing_records %}
* {{ agent.name }}	
{{ agent.description }}
{% if agent.required_fields != empty -%}
Required: {% for field in agent.required_fields %}{{ field }},{% endfor %}
{%- endif %}
{% endfor %}

CONVERSATION";
}
