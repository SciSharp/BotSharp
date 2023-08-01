# How to contribute docs

We use [Sphinx](https://www.sphinx-doc.org/en/master/) to build document, please make sure you installed the appropriate Sphinx plugins before making the docs.

```shell
pip install -U sphinx
pip install recommonmark
pip install sphinx_rtd_theme
pip install myst-parser
cd docs
./make html
```