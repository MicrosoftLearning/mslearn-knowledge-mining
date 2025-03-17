---
title: Azure Knowledge Mining Exercises
permalink: index.html
layout: home
---

# Azure Knowledge Mining Exercises

The following exercises are designed to support the modules on Microsoft Learn.

{% assign labs = site.pages | where_exp:"page", "page.url contains '/Instructions/Exercises'" %}
{% for activity in labs  %}
{% if activity.url contains 'ai-foundry' %}
  {% continue %}
{% endif %}
- [{{ activity.lab.title }}]({{ site.github.url }}{{ activity.url }})
{% endfor %}
