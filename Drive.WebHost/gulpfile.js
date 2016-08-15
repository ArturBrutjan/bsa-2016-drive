/// <binding BeforeBuild='develop' />
var gulp = require("gulp");
var bower = require("gulp-bower");
var concat = require("gulp-concat");
var rimraf = require("rimraf");
var cssmin = require("gulp-cssmin");
var uglify = require("gulp-uglify");
var rename = require("gulp-rename");


gulp.task("prod", ["main", "libs", "styles", "fonts", "minify"]);

gulp.task("develop", ["clean", "bower", "main", "libs", "styles", "fonts"]);
gulp.task("noBower", ["main", "libs", "styles", "fonts"]);

var paths = {
    bower: "bower_components/**/",
    scriptBundles: "./Scripts/Bundles",
    contentBundles: "./Content/Bundles",
    fontBundles: "./Content/Fonts"
};

gulp.task("bower", function () {
    return bower("./bower_components");
});

gulp.task("clean", ["clean:js", "clean:css", "clean:fonts"]);

gulp.task("clean:js", function (cb) {
    rimraf(paths.scriptBundles, cb);
});

gulp.task("clean:css", function (cb) {
    rimraf(paths.contentBundles, cb);
});

gulp.task("clean:fonts", function (cb) {
    rimraf(paths.fontBundles, cb);
});

gulp.task("main", function () {
    gulp.src(["./Scripts/App/*.js", "./Scripts/App/**/*.js"])
        .pipe(concat("main.js"))
        .pipe(gulp.dest(paths.scriptBundles));
});

gulp.task("libs", function () {
    gulp.src([
      paths.bower + "angular.js",
      paths.bower + "angular-route.js",
      paths.bower + "contextMenu.js",
      paths.bower + "massautocomplete.js",
      paths.bower + "ui-bootstrap.js",
      paths.bower + "dist/jquery.js",
      paths.bower + "bootstrap.js"
    ])

        .pipe(concat("libs.js"))
        .pipe(gulp.dest(paths.scriptBundles));
});

gulp.task("styles", function () {
    gulp.src([
        paths.bower + "bootstrap.css",
        paths.bower + "font-awesome.css",
        paths.bower + "font-awesome-animation.css",
        paths.bower + "massautocomplete.theme.css",
        "./Content/*.css"
    ])
        .pipe(concat("styles.css"))
        .pipe(gulp.dest(paths.contentBundles));
});

gulp.task("fonts", function () {
    gulp.src([
        paths.bower + "bootstrap/fonts/*",
        paths.bower + "fontawesome/fonts/*"
    ])
        .pipe(rename({ dirname: '' }))
        .pipe(gulp.dest(paths.fontBundles));
});


gulp.task("minify", ["min:js", "min:css"]);

gulp.task("min:js", function () {
    gulp.src(paths.scriptBundles + "/*.js")
        .pipe(uglify())
        .pipe(rename({
            suffix: ".min"
        }))
        .pipe(gulp.dest(paths.scriptBundles));
});

gulp.task("min:css", function () {
    gulp.src(paths.contentBundles + "/*.css")
        .pipe(cssmin())
        .pipe(rename({
            suffix: ".min"
        }))
        .pipe(gulp.dest(paths.contentBundles));
});