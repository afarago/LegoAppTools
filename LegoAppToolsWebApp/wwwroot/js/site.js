﻿// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

function setSelectedTabByTabTarget(target) {
    const tid = target.id;
    //-- update hidden form field function selector for server processing
    $('#selectedtab').attr('value', tid);

    //-- set submit button visibility based on selected tab
    let is_submit_allowed =
        $(".tab-pane.active").hasClass("_cansubmit") &&
        $("#file").get(0).files.length > 0;
    $("#submitBtn").toggleClass("d-none", !is_submit_allowed);
}
$('a[data-bs-toggle="tab"]').on('shown.bs.tab', function (event) {
    //-- on tab activation update hidden form field for server processing
    setSelectedTabByTabTarget(event.target);
})

$(function () {
    //-- activate active tab
    //const target = $('a[data-bs-toggle="tab"].active')[0];
    //setSelectedTabByTabTarget(target);

    //-- set and wire elements
    $("#file:file").filestyle({
        'onChange': function (files) {
            updateSVGPreview();
        },
        'placeholder': 'Select a file (.llsp, .lms, .lmsp) to upload'
    });
})

function spinnerShow(show) {
    $('#spinner').toggleClass('d-none', { 'duration': 1000, display: show });
}

function updateSVGPreview() {
    const $form = $("form");
    let data1 = new FormData($form.get(0));
    //const selectedtab = data1.get('selectedtab');

    //-- overrde selected tab for posing ajax preview mode
    data1.set('selectedtab', 'preview');

    spinnerShow(true);
    let ajaxRequest = $.getJSON({
        type: "POST",
        url: $form.attr("action"),
        contentType: false,
        processData: false,
        data: data1,
        dataType: "json",
        success: function (response, textStatus, jqXHR) {
            //console.log(response);
            //const content = new XMLSerializer().serializeToString(response);
            const svgcontent = response.svg;
            const statscontent =
                $.map(response.stats, (v, k) => "<tr><td>" + k + "</td><th>" + v.replaceAll("\r\n", "<br>") + "</th>");
            const codecontent = response.code;
            $('#preview_svg').html(svgcontent);
            $('#preview_stats').html(statscontent);
            $('#preview_code').html(codecontent);

            $('#tab_dummy').addClass('d-none');
            $('#tabs_main').removeClass('d-none');
            //    let tab = bootstrap.Tab.getOrCreateInstance(document.querySelector('#tab_code'));
            //    tab.show();
            const is_trgrepair_available = response.stats?.errors?.includes('#ERRTRG1');
            const has_errors = response.stats?.errors?.includes('#ERR');
            const has_warnings = response.stats?.errors?.includes('#WARN');
            $("#repair").toggleClass('d-none', !is_trgrepair_available);
            $("#stats_icon_error").toggleClass('d-none', !has_errors);
            $("#stats_icon_warning").toggleClass('d-none', !has_warnings);

            const slotid = (response.stats?.slot);
            const sloturl = '/img/Cat' + slotid + '.svg#dsmIcon';
            $('#svg_program_use').attr('href', sloturl).attr('xlink:href', sloturl);
        },
        complete: function () {
            spinnerShow(false);
        }
    });
}

//===========================================================
//-- setup drag and drop
$("#maincontainer")
    .on("dragenter", OnDragEnter)
    .on("dragover", OnDragOver)
    .on("dragleave", OnDragLeave)
    .on("drop", OnDrop);

// Drop file handlers
function OnDragEnter(e) {
    e.stopPropagation(); e.preventDefault();
}
function OnDragLeave(e) {
    e.stopPropagation(); e.preventDefault();
    $(this).removeClass("drop-active");
}
function OnDragOver(e) {
    e.stopPropagation(); e.preventDefault();
    $(this).addClass("drop-active");
}
function OnDrop(e) {
    e.stopPropagation(); e.preventDefault();
    $(this).removeClass("drop-active");
    let selectedFiles = e.originalEvent.dataTransfer.files;
    if (!selectedFiles || !selectedFiles.length) return; //-- prevent drop trigger on empty files

    $("#file").get(0).files = selectedFiles;
    $("#file").filestyle('placeholder', Array.from($("#file").get(0).files).map(x => x.name).join(','));
    updateSVGPreview();
}

