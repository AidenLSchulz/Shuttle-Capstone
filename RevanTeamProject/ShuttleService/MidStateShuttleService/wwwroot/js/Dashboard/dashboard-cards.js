$(document).on('click', '.card[data-section-target], .notification-link', function (e) {

    e.preventDefault();

    var section = $(this).data('section-target');

    $('.recentFeedback').hide();

    if (section === 'request') {
        $('.recentFeedback.request').show();
    }
    else if (section === 'check') {
        $('.recentFeedback.check').show();
    }
    else if (section === 'message') {
        $('.recentFeedback.messages').show();
    }
    else if (section === 'feedback') {
        $('.recentFeedback.feedback').show();
    }

    // close notification dropdown after click
    $('.dropdown-menu.notifications').hide();
});