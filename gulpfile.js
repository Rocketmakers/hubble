var gulp = require('gulp');
var del = require('del');
var merge = require('merge-stream');
var args = require('yargs').argv;
var zip = require('gulp-zip');
var path = require('path');
var _ = require('underscore');
var glob = require('glob');
var exec = require('child_process').exec;

var rm_core = require('rocketmake');
var rm_semver = require('rocketmake-semver');
var rm_msbuild = require('rocketmake-msbuild');
var rm_mstest = require('rocketmake-mstest');
var rm_nuget = require('rocketmake-nuget');
var rm_assemblyinfo = require('rocketmake-assemblyinfo');
var rm_logger = require('rocketmake-logger-teamcity');



var structure = rm_core.structure();

var version = {
    semver: '0.0.0-unset',
    dotnetversion: '0.0.0.0'
};

var config = {

    buildType: args.env || 'local',

    solution: 'Hubble.sln',

    outputs: [
        {
            name: 'Hubble',
            locations: 'Hubble/bin/**/*'
        },

        {
            name: 'Hubble.Vsix',
            locations: 'Hubble.Vsix/bin/**/*'
        }
    ],

    nupkgs: function(){
        var apiKey = 'ctdj1COppd7iPafw0RKp';
        var url = 'https://proget.rocketmakers.com/nuget/Default';

        return [
            {
                file: path.join(structure.outputPackagePath, 'Hubble.' + version.semver + '.nupkg'),
                url: url,
                apiKey: apiKey
            }
        ];
    },

    nuspecs: [
        'Nuspec/Hubble/Hubble.nuspec'
    ]

};

gulp.task('getVersion', function(){
    return rm_semver(config.buildType).then(function(v){
        console.log(version = v);
        rm_logger.setVersion(version.semver);
    });
});

gulp.task('cleanWork', function(cb) {
    del([structure.workPath], cb);
});

gulp.task('nugetDownload', rm_nuget.download);

gulp.task('nugetRestore', ['toWork', 'nugetDownload'], function() {
    return rm_nuget.restore({
        solution: path.join(structure.workSourcePath, config.solution)
    });
});

gulp.task('toWork', ['cleanWork'], function() {
    var sourceStream = gulp.src(path.join(structure.sourcePath, '**', '*'), {base: '.'});
    var packageStream = gulp.src(path.join(structure.packagePath, '**', '*'), {base: '.'});

    return merge(sourceStream, packageStream).pipe(gulp.dest(structure.workPath));
});

gulp.task('assemblyInfo', ['toWork', 'getVersion'], function() {
    return rm_assemblyinfo({ version: version, workPath: structure.workPath });
});

gulp.task('build', ['assemblyInfo', 'nugetRestore'], function() {
    return rm_msbuild({
        solution: path.join(structure.workSourcePath, config.solution),
        toolsVersion: 14.0,
        logger: rm_logger.build
    });
});

gulp.task('toBuildOut', ['build'], function() {
    var mergedStreams = merge();

    console.log(config.outputs);

    mergedStreams.add(gulp.src(path.join(structure.sourcePath, '**', '*'))
        .pipe(zip('source.zip'))
        .pipe(gulp.dest(structure.outputBuildPath)));

    for(var i in config.outputs){
        mergedStreams
            .add(gulp
                .src(config.outputs[i].locations, {cwd: structure.workSourcePath})
                .pipe(gulp.dest(config.outputs[i].name, {cwd: structure.outputBuildPath})));
    }

    return mergedStreams;
});

gulp.task('publishArtifacts', ['toBuildOut'], function() {
  console.log("##teamcity[publishArtifacts \'" + path.join(structure.outputBuildPath,  "Hubble.Vsix/Release/Hubble.Vsix.vsix") + "\']");
});

var packFn = function(){
    return rm_nuget.pack({
        outputDir: structure.outputPackagePath,
        packagePath: structure.workPackagePath,
        nuspecs: config.nuspecs,
        basePath: structure.workPath,
        version: version
    });
};

gulp.task('pack', ['nugetDownload', 'toBuildOut'], packFn);

gulp.task('push', ['pack'], function() {
  if (_.contains(['prod', 'test', 'labs', 'demo'], config.buildType)) {
    return rm_nuget.push({ nupkgs: config.nupkgs() });
  }
});

gulp.task('default', ['push', 'publishArtifacts']);
