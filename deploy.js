const fs = require("fs");
const path = require("path");
const OSS = require("ali-oss");

const { IncomingWebhook } = require("@slack/webhook");

const OSS_KEYID = process.env.OSS_ID;
const OSS_KEYSECRET = process.env.OSS_SECRET;
const SLACK_URL = process.env.SLACK_WEBHOOK;
fs.readFile(process.env.GITHUB_EVENT_PATH, (err, data) => {
  if (err) {
    console.log(err);
    return;
  }
  console.log(data);
});
(async () => {
  if (!OSS_KEYID || !OSS_KEYSECRET || !SLACK_URL) {
    console.log(`Can't run deploy.`);
    return;
  }

  console.log("Deploying to Noah server...");

  const webhook = new IncomingWebhook(SLACK_URL);

  const client = new OSS({
    region: "oss-accelerate", // oss-cn-shanghai
    accessKeyId: OSS_KEYID,
    accessKeySecret: OSS_KEYSECRET,
    bucket: "ncfz",
    secure: true
  });

  try {
    const buildDir = "./RH_Plugin/bin";

    if (fs.existsSync(buildDir)) {
      fs.readdirSync(buildDir).forEach(async file => {
        const ext = path.extname(file);
        const filePath = path.join(buildDir, file);
        if (ext === ".rhi" || ext === ".macrhi") {
          let stream = fs.createReadStream(filePath);
          let result = await client.putStream(
            `Noah/Plugin/Rhino/${file}`,
            stream
          );
          console.log(
            `[${result.res.statusCode}] Put ${result.name} to OSS is ${result.res.statusMessage}`
          );
        }
      });
    }

    await webhook.send({
      channel: "noah-client",
      text: `Noah Plugin ${version} 已更新！`
    });

    console.log("Post build action is done !");
  } catch (err) {
    await webhook.send({
      channel: "noah-client",
      text: `Noah Plugin ${version} deploy faild！`
    });

    console.log(err);
  }
})();
