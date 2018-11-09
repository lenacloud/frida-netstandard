
rpc.exports.pingScript = function (x) {
    return { message: 'Pong back to ' + x.author };
}
rpc.exports.longOperation = function () {
    Thread.sleep(2);
}

console.log('Script initialized');
