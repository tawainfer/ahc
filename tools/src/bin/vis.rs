use tools::{draw_svg, judge, Input};

fn main() {
    if std::env::args().len() != 3 {
        eprintln!(
            "Usage: {} <input> <output>",
            std::env::args().nth(0).unwrap()
        );
        return;
    }
    let in_file = std::env::args().nth(1).unwrap();
    let out_file = std::env::args().nth(2).unwrap();

    let input_s = std::fs::read_to_string(&in_file).unwrap_or_else(|_| {
        eprintln!("no such file: {}", in_file);
        std::process::exit(1)
    });
    let output_s = std::fs::read_to_string(&out_file).unwrap_or_else(|_| {
        eprintln!("no such file: {}", out_file);
        std::process::exit(1)
    });

    let mut vis_data_vec = vec![];

    let mut input_res = Input::new();

    let k = 10;
    let svg = match judge(&input_s, &output_s, &mut vis_data_vec, &mut input_res, k) {
        Ok(judge_result) => {
            eprintln!("Score = {}", judge_result.score);

            let vis_data = vis_data_vec.last().unwrap();
            draw_svg(&vis_data, &input_res, k).to_string()
        }
        Err(err) => {
            eprintln!("{:#}", err);
            eprintln!("Score = 0");

            String::new()
        }
    };

    let vis = format!("<html><body>{}</body></html>", svg);
    std::fs::write("vis.html", &vis).unwrap();
}
