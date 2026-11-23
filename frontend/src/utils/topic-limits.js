/**
 * Limits on a topic. The question limits mirror `DefaultSettings.cs` on the server.
 * `titleMax` is the topic form's own limit; the server doesn't check titles yet.
 */
export const TOPIC_LIMITS = {
  titleMax: 100,
  questionsPerTopicMin: 1,
  questionsPerTopicMax: 10,
  questionsCostMin: 1,
  questionsCostMax: 150,
  questionsCostPerTopicMax: 150,
};
