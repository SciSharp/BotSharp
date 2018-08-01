var webpack = require("webpack");
var path = require('path');
var uglifyJsPlugin = webpack.optimize.UglifyJsPlugin;
var ExtractTextPlugin = require("extract-text-webpack-plugin");
var HtmlWebpackPlugin = require('html-webpack-plugin');

var ROOT_PATH = path.resolve(__dirname); // 项目跟路径
var APP_PATH = path.resolve(ROOT_PATH, 'Views'); // 项目开发目录src
var APP_FILE = path.resolve(APP_PATH, 'index.js'); // 项目入口的index.js
var DIST_PATH = path.resolve(ROOT_PATH, 'dist'); // 项目打包输出路径

module.exports = {
  entry: {
    app: APP_FILE,
    venders: [
      'react',
      'react-dom',
      'react-router',
      // 'antd',这里直接会把所有antd打包
      'mockjs',
      'babel-polyfill'
    ]
  },
  output: {
    path: DIST_PATH,
    filename: '[name].js',
    chunkFilename: 'js/[name].[chunkhash:5].min.js'
  },
  module: {
    loaders: [
      {
        test: /\.css$/,
        loader: ExtractTextPlugin.extract('style', ['css', 'autoprefixer'])
      },
      {
        test: /\.less$/,
        loader: ExtractTextPlugin.extract('style', ['css', 'autoprefixer', 'less'])
      },
      {
        test: /\.jsx?$/,
        exclude: /node_modules/,
        loaders: ['react-hot', 'babel?presets[]=es2015,presets[]=react,presets[]=stage-0']
      },
      {
        test: /\.(png|jpg)$/,
        loader: 'file-loader?limit=8192&name=images/[hash:8].[name].[ext]'
      },
      {
        test: /\.(woff|woff2|eot|ttf|svg)(\?.*$|$)/,
        loader: 'url'
      }
    ]
  },
  resolve: {
    extensions: ['', '.js', '.jsx', '.json', '.less']
  },
  plugins: [
    // new webpack.ProvidePlugin({ $: "jquery" }), // 这是将jquery变成全局变量，不用在自己文件require('jquery')了
    new webpack.DefinePlugin({
      'process.env': {
        NODE_ENV: JSON.stringify('production') //定义生产环境
      }
    }),
    new webpack.optimize.CommonsChunkPlugin('venders', 'venders.js'), // 这是第三方库打包生成的文件
    new uglifyJsPlugin({
      output: {
        comments: false, // remove all comments
      },
      compress: {
        warnings: false
      }
    }),
    new ExtractTextPlugin("[name].css"),
    new HtmlWebpackPlugin({
      template: './Views/template/template.html', // html模板路径
      htmlWebpackPlugin: {
        files: {
          css: ["app.css"],
          js: ["bundle.js", "venders.js"]
        }
      },
      minify: {
        removeComments: true,
        collapseWhitespace: true, // 删除空白符与换行符
        removeAttributeQuotes: true // 移除HTML中的属性引号
      },
      filename: 'index.html',
      favicon:'./Views/favicon.ico', //favicon路径
    })
  ]
};
